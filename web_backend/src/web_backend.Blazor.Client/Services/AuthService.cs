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
                await _debugService.LogAsync("AuthService: Starting login process...");
                
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    await _debugService.LogAsync("AuthService: API base URL is missing from configuration");
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

                await _debugService.LogAsync($"AuthService: Sending login request to {loginEndpoint}");
                
                // Add appropriate headers for the request
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, loginEndpoint);
                requestMessage.Content = JsonContent.Create(loginRequest);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Send the login request with proper credentials
                var response = await _httpClient.SendAsync(requestMessage);
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Login failed with status {response.StatusCode}: {errorContent}");
                    return false;
                }

                // Parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                await _debugService.LogAsync($"AuthService: Login response received with status: {response.StatusCode}");
                
                // Store authentication information in the browser
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", "true");
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                
                // Fetch the current user info to store locally
                await _debugService.LogAsync("AuthService: Fetching current user info");
                await FetchAndStoreCurrentUser();
                
                // Notify authentication state changed
                await _debugService.LogAsync("AuthService: Notifying authentication state changed");
                _authStateProvider.NotifyAuthenticationStateChanged();
                
                // Small delay to ensure UI updates
                await Task.Delay(300); 
                
                await _debugService.LogAsync("AuthService: Login process completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Login exception: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _debugService.LogAsync("AuthService: Starting logout process");
                
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    apiBaseUrl = "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";
                }

                // Call the logout endpoint to invalidate the server-side session
                try
                {
                    await _debugService.LogAsync($"AuthService: Calling logout endpoint at {apiBaseUrl}/api/account/logout");
                    await _httpClient.PostAsync($"{apiBaseUrl}/api/account/logout", null);
                }
                catch (Exception ex)
                {
                    await _debugService.LogAsync($"AuthService: Error calling logout endpoint: {ex.Message}");
                    // Continue with client-side logout even if the server call fails
                }
                
                // Clear the authentication data in local storage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "isAuthenticated");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authTime");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
                await _jsRuntime.InvokeVoidAsync("authTokenManager.clearAuthData");
                
                // Notify authentication state changed
                _authStateProvider.NotifyAuthenticationStateChanged();
                
                await _debugService.LogAsync("AuthService: Logout completed");
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Logout exception: {ex.Message}");
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                await _debugService.LogAsync("AuthService: Checking authentication state");

                // Check if auth cookie exists (ABP uses cookie authentication)
                var isAuthenticatedInStorage = await _jsRuntime.InvokeAsync<bool>("eval", 
                    "localStorage.getItem('isAuthenticated') === 'true'");
                
                // If we're authenticated, check if we need to refresh user info
                if (isAuthenticatedInStorage)
                {
                    var authTimeStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authTime");
                    if (!string.IsNullOrEmpty(authTimeStr) && DateTime.TryParse(authTimeStr, out var authTime))
                    {
                        // If it's been more than 5 minutes since last auth check, refresh user info
                        if ((DateTime.UtcNow - authTime).TotalMinutes > 5)
                        {
                            await _debugService.LogAsync("AuthService: Auth data is stale, refreshing");
                            
                            // Fetch fresh user info to verify authentication is still valid
                            var isStillAuthenticated = await FetchAndStoreCurrentUser();
                            if (!isStillAuthenticated)
                            {
                                await _debugService.LogAsync("AuthService: User is no longer authenticated");
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "isAuthenticated");
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authTime");
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
                                
                                // Notify authentication state changed
                                _authStateProvider.NotifyAuthenticationStateChanged();
                                return false;
                            }
                            
                            // Update auth time
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                        }
                    }
                    
                    await _debugService.LogAsync("AuthService: User is authenticated");
                    return true;
                }
                
                await _debugService.LogAsync("AuthService: User is not authenticated");
                return false;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: IsAuthenticated exception: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> FetchAndStoreCurrentUser()
        {
            try
            {
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    apiBaseUrl = "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";
                }
                
                // Fetch current user profile
                var profileEndpoint = $"{apiBaseUrl}/api/account/my-profile";
                await _debugService.LogAsync($"AuthService: Fetching user profile from {profileEndpoint}");
                
                var response = await _httpClient.GetAsync(profileEndpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    await _debugService.LogAsync($"AuthService: Failed to fetch user profile: {response.StatusCode}");
                    return false;
                }
                
                var userInfo = await response.Content.ReadAsStringAsync();
                await _debugService.LogAsync("AuthService: Successfully fetched user profile");
                
                // Store user info in local storage
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", userInfo);
                
                return true;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Error fetching user profile: {ex.Message}");
                return false;
            }
        }
    }
}