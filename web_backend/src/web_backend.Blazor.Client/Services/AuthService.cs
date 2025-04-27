using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly TokenAuthenticationStateProvider _authStateProvider;

        public AuthService(
            HttpClient httpClient, 
            IJSRuntime jsRuntime, 
            IConfiguration configuration,
            TokenAuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var authServerUrl = _configuration["AuthServer:Authority"];
                var clientId = _configuration["AuthServer:ClientId"];
                
                if (string.IsNullOrEmpty(authServerUrl) || string.IsNullOrEmpty(clientId))
                {
                    Console.Error.WriteLine("Auth server configuration is missing");
                    return false;
                }

                // Prepare the token endpoint URL
                var tokenEndpoint = $"{authServerUrl}/connect/token";
                
                // Prepare the request content
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("scope", "web_backend email openid profile role phone")
                });

                // Send the token request
                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Login failed: {errorContent}");
                    return false;
                }

                // Parse the response
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    Console.Error.WriteLine("Invalid token response");
                    return false;
                }

                // Store the tokens in local storage using JavaScript
                await _jsRuntime.InvokeVoidAsync(
                    "authTokenManager.storeAuthData",
                    tokenResponse.AccessToken,
                    tokenResponse.RefreshToken,
                    tokenResponse.ExpiresIn
                );

                // Notify authentication state changed
                _authStateProvider.NotifyAuthenticationStateChanged();

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Login exception: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                // Clear the authentication data
                await _jsRuntime.InvokeVoidAsync("authTokenManager.clearAuthData");
                
                // Notify authentication state changed
                _authStateProvider.NotifyAuthenticationStateChanged();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Logout exception: {ex.Message}");
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("authTokenManager.isAuthenticated");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"IsAuthenticated exception: {ex.Message}");
                return false;
            }
        }

        private class TokenResponse
        {
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
            public string? TokenType { get; set; }
        }
    }
}