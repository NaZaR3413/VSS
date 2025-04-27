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
using Volo.Abp.DependencyInjection;

namespace web_backend.Blazor.Client.Services
{
    public class AuthService : ITransientDependency
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly TokenAuthenticationStateProvider _authStateProvider;
        private readonly DebugService _debugService;

        public AuthService(
            HttpClient httpClient, 
            IJSRuntime jsRuntime, 
            IConfiguration configuration,
            TokenAuthenticationStateProvider authStateProvider,
            DebugService debugService)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _authStateProvider = authStateProvider;
            _debugService = debugService;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                await _debugService.LogAsync("Starting login process...");
                
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    await _debugService.LogAsync("API base URL is missing from configuration");
                    return false;
                }

                // Use the ABP account login endpoint
                var loginEndpoint = $"{apiBaseUrl}/api/account/login";
                
                // Create login request with credentials
                var loginRequest = new
                {
                    UserNameOrEmailAddress = username,
                    Password = password,
                    RememberMe = true
                };

                await _debugService.LogAsync($"Sending login request to {loginEndpoint}");
                
                // Add XSRF header if needed
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, loginEndpoint);
                requestMessage.Content = JsonContent.Create(loginRequest);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Send the login request
                var response = await _httpClient.SendAsync(requestMessage);
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"Login failed: {errorContent}");
                    return false;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                await _debugService.LogAsync($"Login response received with status: {response.StatusCode}");
                
                // Try to parse as token response (in case the API returns a token)
                try
                {
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
                        
                        await _debugService.LogAsync("Token stored successfully");
                    }
                    else
                    {
                        await _debugService.LogAsync("No token in response, using cookie-based auth");
                    }
                }
                catch (Exception ex)
                {
                    // If we can't parse as token response, it might be a success response without a token
                    await _debugService.LogAsync($"Using cookie-based auth. Parse error: {ex.Message}");
                }

                // For ABP cookie-based auth, get and store user info
                try
                {
                    await _debugService.LogAsync("Fetching current user info after login");
                    var userInfoResponse = await _httpClient.GetAsync($"{apiBaseUrl}/api/account/my-profile");
                    
                    if (userInfoResponse.IsSuccessStatusCode)
                    {
                        var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", userInfo);
                        await _debugService.LogAsync("User profile stored in local storage");
                    }
                }
                catch (Exception ex)
                {
                    await _debugService.LogAsync($"Error fetching user profile: {ex.Message}");
                }

                // Notify authentication state changed
                await _debugService.LogAsync("Notifying authentication state changed");
                _authStateProvider.NotifyAuthenticationStateChanged();
                
                // Small delay to ensure UI updates
                await Task.Delay(200); 
                
                return true;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"Login exception: {ex.Message}");
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