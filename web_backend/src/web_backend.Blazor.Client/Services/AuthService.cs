using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

                // Create a DelegatingHandler to capture and log cookies
                var cookieHandler = new CookieCaptureHandler();
                
                // Create a custom HttpClient using our cookie capturing handler
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };
                
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiBaseUrl)
                };
                
                // Add headers to mimic a browser request
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Blazor WebAssembly)");
                client.DefaultRequestHeaders.Add("Origin", "https://salmon-glacier-08dca301e.6.azurestaticapps.net");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Create login request with credentials
                var loginRequest = new
                {
                    UserNameOrEmailAddress = username,
                    Password = password,
                    RememberMe = true
                };

                await _debugService.LogAsync($"AuthService: Sending login request for user: {username}");
                
                // Convert to JSON
                var jsonContent = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Send login request
                var response = await client.PostAsync("/api/account/login", content);
                
                await _debugService.LogAsync($"AuthService: Login response status: {(int)response.StatusCode} {response.StatusCode}");
                
                // Check for cookies in the response
                if (response.Headers.Contains("Set-Cookie"))
                {
                    var cookies = response.Headers.GetValues("Set-Cookie").ToList();
                    await _debugService.LogAsync($"AuthService: Received {cookies.Count} cookies from server");
                    
                    foreach (var cookie in cookies)
                    {
                        await _debugService.LogAsync($"AuthService: Cookie: {cookie}");
                    }
                    
                    // Store all cookies for later use
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authCookies", 
                        JsonSerializer.Serialize(cookies));
                    
                    // Manually apply each relevant cookie to document.cookie
                    foreach (var cookie in cookies)
                    {
                        try
                        {
                            var mainPart = cookie.Split(';')[0];
                            await _jsRuntime.InvokeVoidAsync("eval", 
                                $"document.cookie = '{mainPart}; path=/; SameSite=None; Secure';");
                            await _debugService.LogAsync($"AuthService: Applied cookie to document: {mainPart}");
                        }
                        catch (Exception ex)
                        {
                            await _debugService.LogAsync($"AuthService: Error applying cookie: {ex.Message}");
                        }
                    }
                    
                    // Mark as authenticated in local storage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                    
                    // Debug log to check document.cookie
                    var documentCookies = await _jsRuntime.InvokeAsync<string>("eval", "document.cookie");
                    await _debugService.LogAsync($"AuthService: Current document.cookie: {documentCookies}");
                    
                    // Try to fetch user profile info
                    await _debugService.LogAsync("AuthService: Fetching user profile after login");
                    var userProfile = await FetchCurrentUserProfile(cookies);
                    
                    // Store user profile if successful
                    if (!string.IsNullOrWhiteSpace(userProfile))
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", userProfile);
                        await _debugService.LogAsync("AuthService: Stored user profile in localStorage");
                    }
                    
                    // Notify auth state provider
                    await _debugService.LogAsync("AuthService: Notifying auth state changed");
                    _authStateProvider.NotifyAuthenticationStateChanged();
                    
                    // Small delay to ensure UI updates
                    await Task.Delay(200);
                    
                    return true;
                }
                else if (response.IsSuccessStatusCode)
                {
                    // Response is successful but no cookies - try to parse the response content
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Login response content: {responseContent}");
                    
                    try
                    {
                        // Store in localStorage
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", responseContent);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", "true");
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                        
                        // Notify auth state provider
                        _authStateProvider.NotifyAuthenticationStateChanged();
                        
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _debugService.LogAsync($"AuthService: Error parsing login response: {ex.Message}");
                    }
                }
                
                await _debugService.LogAsync("AuthService: Login failed - no auth cookies received");
                return false;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Login exception: {ex.Message}");
                await _debugService.LogAsync($"AuthService: Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<string> FetchCurrentUserProfile(List<string> cookies)
        {
            try
            {
                // Get API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    apiBaseUrl = "https://vss-backend-api-fmbjgachhph9byce.westus2-01.azurewebsites.net";
                }
                
                // Create HttpClient with our cookie handling
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };
                
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiBaseUrl)
                };
                
                // Add headers to request
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Blazor WebAssembly)");
                client.DefaultRequestHeaders.Add("Origin", "https://salmon-glacier-08dca301e.6.azurestaticapps.net");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Add cookies to request
                client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Select(c => c.Split(';')[0])));
                
                // Request user profile
                await _debugService.LogAsync("AuthService: Sending request to /api/account/my-profile");
                var response = await client.GetAsync("/api/account/my-profile");
                
                await _debugService.LogAsync($"AuthService: Profile response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var userProfile = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Received profile: {userProfile}");
                    return userProfile;
                }
                else if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    // Handle redirect
                    var location = response.Headers.Location?.ToString();
                    await _debugService.LogAsync($"AuthService: Profile request redirected to: {location}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Profile request failed: {errorContent}");
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Error fetching profile: {ex.Message}");
                return string.Empty;
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

                // Use the correct ABP logout endpoint per your API list
                try
                {
                    await _debugService.LogAsync($"AuthService: Calling ABP logout endpoint");
                    
                    // Create a handler that uses cookies
                    using var handler = new HttpClientHandler
                    {
                        UseCookies = true
                    };
                    
                    using var client = new HttpClient(handler)
                    {
                        BaseAddress = new Uri(apiBaseUrl)
                    };
                    
                    // Get stored cookies to include with request
                    var cookies = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authCookies");
                    if (!string.IsNullOrEmpty(cookies))
                    {
                        client.DefaultRequestHeaders.Add("Cookie", cookies);
                    }
                    
                    // Send GET request to logout endpoint as per your API list
                    await client.GetAsync("/api/account/logout");
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
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authCookies");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "tokenExpiration");
                
                // Clear cookies
                await _jsRuntime.InvokeVoidAsync("eval", 
                    "document.cookie.split(';').forEach(function(c) { " +
                    "document.cookie = c.replace(/^ +/, '').replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/'); " +
                    "});");
                
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
                    "localStorage.getItem('isAuthenticated') === 'true' || " +
                    "document.cookie.indexOf('.AspNetCore.Identity.Application') >= 0");
                
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
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authCookies");
                                
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
                
                // Create a custom HttpClient that doesn't follow redirects
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = true
                };
                
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiBaseUrl)
                };
                
                // Apply stored cookies
                var cookies = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authCookies");
                if (!string.IsNullOrEmpty(cookies))
                {
                    client.DefaultRequestHeaders.Add("Cookie", cookies);
                }
                
                // Add CORS headers
                client.DefaultRequestHeaders.Add("Origin", "https://salmon-glacier-08dca301e.6.azurestaticapps.net");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Use the correct ABP endpoint for fetching the user profile per your API list
                var profileEndpoint = "/api/account/my-profile";
                await _debugService.LogAsync($"AuthService: Fetching user profile from {apiBaseUrl}{profileEndpoint}");
                
                var response = await client.GetAsync(profileEndpoint);
                
                // Check if it's a redirect to login page (which means we're not authenticated)
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    var redirectUrl = response.Headers.Location?.ToString();
                    if (redirectUrl != null && redirectUrl.Contains("Login"))
                    {
                        await _debugService.LogAsync("AuthService: Redirected to login page - not authenticated");
                        return false;
                    }
                }
                
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

    // Helper class to capture cookies
    public class CookieCaptureHandler : DelegatingHandler
    {
        public List<string> CapturedCookies { get; } = new List<string>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            
            if (response.Headers.Contains("Set-Cookie"))
            {
                CapturedCookies.AddRange(response.Headers.GetValues("Set-Cookie"));
            }
            
            return response;
        }
    }
}