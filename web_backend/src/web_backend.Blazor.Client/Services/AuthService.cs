using System;
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
                await _debugService.LogAsync("AuthService: *** STARTING LOGIN PROCESS ***");
                
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    await _debugService.LogAsync("AuthService: API base URL is missing, using hardcoded default");
                    apiBaseUrl = "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";
                }
                await _debugService.LogAsync($"AuthService: Using API URL: {apiBaseUrl}");
                
                // Create login request with credentials
                var loginRequest = new
                {
                    UserNameOrEmailAddress = username,
                    Password = password,
                    RememberMe = true
                };
                
                await _debugService.LogAsync($"AuthService: Sending login request for user: {username}");
                
                // Use the provided HttpClient - configured properly for Blazor WebAssembly
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest"); // Add AJAX header to prevent redirect
                
                // Send the login request
                var response = await _httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/account/login", loginRequest);
                
                await _debugService.LogAsync($"AuthService: Login response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    // For Blazor WebAssembly, we'll store and use token-based authentication 
                    // instead of cookies, which are more difficult to handle in the browser
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Login response content: {responseContent}");
                    
                    // Store authentication state in localStorage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                    
                    // Try to parse the response to see if it contains user info
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        try
                        {
                            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                            if (result.TryGetProperty("result", out var resultContent))
                            {
                                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", resultContent.ToString());
                                await _debugService.LogAsync("AuthService: Stored user profile from login response");
                            }
                            else
                            {
                                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", responseContent);
                                await _debugService.LogAsync("AuthService: Stored raw response as user profile");
                            }
                        }
                        catch
                        {
                            // If we can't parse as JSON, just store the raw response
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", responseContent);
                        }
                    }
                    
                    // Fetch user profile separately as a backup
                    await _debugService.LogAsync("AuthService: Fetching user profile after login");
                    await FetchAndStoreCurrentUser();
                    
                    // Notify auth state provider of the change
                    _authStateProvider.NotifyAuthenticationStateChanged();
                    
                    await _debugService.LogAsync("AuthService: Login completed successfully");
                    return true;
                }
                else
                {
                    // Try to read error information
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Login failed. Server response: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Login exception: {ex.Message}");
                await _debugService.LogAsync($"AuthService: Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        private async Task<bool> FetchAndStoreCurrentUser()
        {
            try
            {
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"] ?? 
                                "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";
                
                await _debugService.LogAsync("AuthService: Sending request to /api/account/my-profile");
                
                // Use the existing HttpClient
                var response = await _httpClient.GetAsync($"{apiBaseUrl}/api/account/my-profile");
                
                await _debugService.LogAsync($"AuthService: Profile response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var userProfile = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Received profile: {userProfile}");
                    
                    // Store user info in local storage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", userProfile);
                    await _debugService.LogAsync("AuthService: Stored user profile in localStorage");
                    
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Profile request failed: {errorContent}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Error fetching profile: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _debugService.LogAsync("AuthService: Starting logout process");
                
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"] ?? 
                               "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";

                try
                {
                    // Call the logout endpoint
                    await _httpClient.GetAsync($"{apiBaseUrl}/api/account/logout");
                    await _debugService.LogAsync("AuthService: Logout request sent to server");
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
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
                
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

                // Check if we're marked as authenticated in localStorage
                var isLocalStorageAuth = await _jsRuntime.InvokeAsync<bool>("eval", 
                    "localStorage.getItem('isAuthenticated') === 'true'");
                
                if (isLocalStorageAuth)
                {
                    var authTimeStr = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authTime");
                    if (!string.IsNullOrEmpty(authTimeStr) && DateTime.TryParse(authTimeStr, out var authTime))
                    {
                        // If it's been more than 30 minutes since last auth check, refresh user info
                        if ((DateTime.UtcNow - authTime).TotalMinutes > 30)
                        {
                            await _debugService.LogAsync("AuthService: Auth data is stale, refreshing");
                            
                            // Verify authentication with the server
                            var result = await FetchAndStoreCurrentUser();
                            if (!result)
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
    }
}