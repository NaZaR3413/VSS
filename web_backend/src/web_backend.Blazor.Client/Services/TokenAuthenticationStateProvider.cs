using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    public class TokenAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private readonly DebugService _debugService;

        public TokenAuthenticationStateProvider(
            IJSRuntime jsRuntime,
            HttpClient httpClient,
            DebugService debugService)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _debugService = debugService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                await _debugService.LogAsync("Getting authentication state");
                
                // Check if we have a token in local storage via JS
                var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authTokenManager.isAuthenticated");
                
                if (!isAuthenticated)
                {
                    await _debugService.LogAsync("No valid token found, returning anonymous user");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                
                // Get the token from local storage
                var token = await _jsRuntime.InvokeAsync<string>("authTokenManager.getAccessToken");
                
                if (string.IsNullOrEmpty(token))
                {
                    await _debugService.LogAsync("Token is empty, returning anonymous user");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                
                // Create authenticated user
                await _debugService.LogAsync("Token found, creating authenticated user");
                
                // Parse the JWT token to get claims
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                
                // Add Authorization header to HttpClient for subsequent requests
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"Error in GetAuthenticationStateAsync: {ex.Message}");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var claimsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (claimsDict != null)
            {
                foreach (var keyValuePair in claimsDict)
                {
                    if (keyValuePair.Value != null)
                    {
                        var value = keyValuePair.Value.ToString() ?? string.Empty;
                        
                        // Handle array claims (like roles)
                        if (value.StartsWith("[") && value.EndsWith("]"))
                        {
                            try
                            {
                                var arrayValues = JsonSerializer.Deserialize<string[]>(value);
                                if (arrayValues != null)
                                {
                                    foreach (var arrayValue in arrayValues)
                                    {
                                        claims.Add(new Claim(keyValuePair.Key, arrayValue));
                                    }
                                    continue;
                                }
                            }
                            catch
                            {
                                // If deserialize fails, treat as a regular string claim
                            }
                        }
                        
                        claims.Add(new Claim(keyValuePair.Key, value));
                    }
                }
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}