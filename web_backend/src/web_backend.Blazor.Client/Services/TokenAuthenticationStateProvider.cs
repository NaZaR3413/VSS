using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using Volo.Abp.DependencyInjection;

namespace web_backend.Blazor.Client.Services
{
    public class TokenAuthenticationStateProvider : AuthenticationStateProvider, ITransientDependency
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly DebugService _debugService;

        // Event that allows components to subscribe to auth state changes
        public event AuthenticationStateChangedHandler AuthenticationStateChanged;
        public delegate void AuthenticationStateChangedHandler(Task<AuthenticationState> task);

        public TokenAuthenticationStateProvider(IJSRuntime jsRuntime, DebugService debugService)
        {
            _jsRuntime = jsRuntime;
            _debugService = debugService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            await _debugService.LogAsync("TokenAuthProvider: Getting authentication state");
            
            try
            {
                // First check if ABP auth cookie exists - this is the most reliable way
                // The ABP auth cookie is typically named .AspNetCore.Identity.Application
                var hasCookie = await _jsRuntime.InvokeAsync<bool>("eval", 
                    "document.cookie.indexOf('.AspNetCore.Identity.Application') >= 0 || localStorage.getItem('isAuthenticated') === 'true'");
                
                if (!hasCookie)
                {
                    await _debugService.LogAsync("TokenAuthProvider: No auth cookie found - not authenticated");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Check if we have user info stored
                var userInfoJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentUser");
                
                if (string.IsNullOrEmpty(userInfoJson))
                {
                    // Try to get user from token if available
                    var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "accessToken");
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        var claims = ParseClaimsFromJwt(token);
                        var identity = new ClaimsIdentity(claims, "jwt");
                        await _debugService.LogAsync("TokenAuthProvider: Created identity from JWT token");
                        return new AuthenticationState(new ClaimsPrincipal(identity));
                    }
                    
                    await _debugService.LogAsync("TokenAuthProvider: Auth cookie exists but no user info available - creating minimal identity");
                    
                    // If we have a cookie but no user info, create a minimal authenticated identity
                    // This at least shows the user as logged in even if we don't have detailed info
                    var minimalClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "User"),
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    };
                    
                    var minimalIdentity = new ClaimsIdentity(minimalClaims, "cookie");
                    return new AuthenticationState(new ClaimsPrincipal(minimalIdentity));
                }

                // Parse the user info from JSON
                try
                {
                    await _debugService.LogAsync("TokenAuthProvider: Parsing user info from storage");
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);
                    
                    // Build claims from user info
                    var claims = new List<Claim>();
                    
                    // Always add a name claim
                    if (userInfo.TryGetProperty("userName", out var userName))
                    {
                        claims.Add(new Claim(ClaimTypes.Name, userName.GetString()));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, userName.GetString()));
                    }
                    else if (userInfo.TryGetProperty("name", out var name))
                    {
                        claims.Add(new Claim(ClaimTypes.Name, name.GetString()));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, name.GetString()));
                    }
                    
                    // Add email claim if available
                    if (userInfo.TryGetProperty("email", out var email))
                    {
                        claims.Add(new Claim(ClaimTypes.Email, email.GetString()));
                    }
                    
                    // Add roles if available
                    if (userInfo.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString()));
                        }
                    }
                    
                    // Include all other properties as claims
                    foreach (var prop in userInfo.EnumerateObject())
                    {
                        if (prop.Name != "userName" && prop.Name != "name" && 
                            prop.Name != "email" && prop.Name != "roles")
                        {
                            claims.Add(new Claim(prop.Name, prop.Value.ToString()));
                        }
                    }
                    
                    // Create the identity and return authentication state
                    var identity = new ClaimsIdentity(claims, "abp");
                    await _debugService.LogAsync($"TokenAuthProvider: Created identity with {claims.Count} claims");
                    
                    return new AuthenticationState(new ClaimsPrincipal(identity));
                }
                catch (Exception ex)
                {
                    await _debugService.LogAsync($"TokenAuthProvider: Error parsing user info: {ex.Message}");
                }
                
                await _debugService.LogAsync("TokenAuthProvider: Fallback to unauthenticated state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"TokenAuthProvider: Error in GetAuthenticationStateAsync: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            var authStateTask = GetAuthenticationStateAsync();
            base.NotifyAuthenticationStateChanged(authStateTask);
            
            // Also notify our custom subscribers
            AuthenticationStateChanged?.Invoke(authStateTask);
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