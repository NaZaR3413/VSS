using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    Console.Error.WriteLine("API base URL is missing from configuration");
                    return false;
                }

                // Use the correct login endpoint from your API
                var loginEndpoint = $"{apiBaseUrl}/api/account/login";
                
                // Create login request with credentials
                var loginRequest = new
                {
                    UserNameOrEmailAddress = username,
                    Password = password,
                    RememberMe = true
                };

                // Send the login request as JSON
                var response = await _httpClient.PostAsJsonAsync(loginEndpoint, loginRequest);
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Login failed: {errorContent}");
                    return false;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Login response: {responseContent}");
                
                try
                {
                    // Try to parse as token response
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        // Store the tokens in local storage
                        await _jsRuntime.InvokeVoidAsync(
                            "authTokenManager.storeAuthData",
                            tokenResponse.AccessToken,
                            tokenResponse.RefreshToken ?? string.Empty,
                            tokenResponse.ExpiresIn
                        );
                        
                        // Log successful token storage
                        Console.WriteLine("Token stored successfully");
                    }
                    else
                    {
                        Console.WriteLine("Login successful but no token in response. Cookie-based auth may be in use.");
                    }
                }
                catch
                {
                    // If we can't parse as token response, it might be a success response without a token
                    // This is fine in cookie-based auth
                    Console.WriteLine("Login successful, using cookie-based authentication");
                }

                // Notify authentication state changed regardless of token or cookie auth
                _authStateProvider.NotifyAuthenticationStateChanged();
                await Task.Delay(100); // Small delay to ensure state is updated
                
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