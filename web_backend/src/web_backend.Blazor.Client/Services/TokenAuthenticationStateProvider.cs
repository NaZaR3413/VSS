using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;

namespace web_backend.Blazor.Client.Services
{
    public class TokenAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private bool _initialized = false;
        private AuthenticationState _latestState;

        public TokenAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _latestState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Always check authentication state from JS storage
            _latestState = await GetAuthenticationStateFromJsAsync();
            _initialized = true;
            return _latestState;
        }

        public void NotifyAuthenticationStateChanged()
        {
            _initialized = false;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private async Task<AuthenticationState> GetAuthenticationStateFromJsAsync()
        {
            try
            {
                var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authTokenManager.isAuthenticated");
                
                if (!isAuthenticated)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var token = await _jsRuntime.InvokeAsync<string>("authTokenManager.getAccessToken");
                if (string.IsNullOrEmpty(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Parse the JWT token to extract claims
                var claims = ParseClaimsFromJwt(token);
                
                // Create the authenticated user identity
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting auth state: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            
            // The JWT consists of three parts separated by dots
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                // If the token doesn't have 3 parts, it's not valid
                return claims;
            }

            // The second part is the payload (claims)
            var payload = parts[1];
            
            // Add padding if needed
            var padding = 4 - (payload.Length % 4);
            if (padding < 4)
            {
                payload = payload + new string('=', padding);
            }

            // Base64 decode the payload
            var bytes = Convert.FromBase64String(payload);
            var jsonPayload = System.Text.Encoding.UTF8.GetString(bytes);
            
            // Parse the JSON
            using (var jsonDocument = System.Text.Json.JsonDocument.Parse(jsonPayload))
            {
                var root = jsonDocument.RootElement;
                
                foreach (var property in root.EnumerateObject())
                {
                    var claimValue = property.Value.ToString();
                    var claimType = property.Name;
                    
                    // Map JWT standard claims to their corresponding ClaimTypes
                    if (property.Name == "sub")
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
                    }
                    else if (property.Name == "unique_name" || property.Name == "name")
                    {
                        claims.Add(new Claim(ClaimTypes.Name, claimValue));
                    }
                    else if (property.Name == "email")
                    {
                        claims.Add(new Claim(ClaimTypes.Email, claimValue));
                    }
                    else if (property.Name == "role" || property.Name == "roles")
                    {
                        if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var role in property.Value.EnumerateArray())
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                            }
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, claimValue));
                        }
                    }
                    else
                    {
                        // Add all other claims as-is
                        claims.Add(new Claim(claimType, claimValue));
                    }
                }
            }

            return claims;
        }
    }
}