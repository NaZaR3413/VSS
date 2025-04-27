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
                await _debugService.LogAsync("AuthService: Starting login process...");
                
                // Get base API URL from configuration
                var apiBaseUrl = _configuration["RemoteServices:Default:BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    await _debugService.LogAsync("AuthService: API base URL is missing from configuration");
                    return false;
                }

                // Use the standard ABP login endpoint per your API list
                var loginEndpoint = $"{apiBaseUrl}/api/account/login";
                
                // Create login request with credentials using ABP format
                var loginRequest = new
                {
                    UserNameOrEmailAddress = username,
                    Password = password,
                    RememberMe = true
                };

                await _debugService.LogAsync($"AuthService: Sending login request to {loginEndpoint}");
                
                // Create a custom HttpClient that doesn't follow redirects automatically
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = true
                };
                
                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(apiBaseUrl)
                };
                
                // Add CORS headers
                client.DefaultRequestHeaders.Add("Origin", "https://salmon-glacier-08dca301e.6.azurestaticapps.net");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Send the login request with proper credentials
                var jsonContent = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("/api/account/login", content);
                
                // Check if the request was successful (2xx) or if it's a redirect (3xx)
                if (response.IsSuccessStatusCode || (int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    await _debugService.LogAsync($"AuthService: Login response received with status: {response.StatusCode}");
                    
                    // Extract cookies from response if they exist
                    if (response.Headers.Contains("Set-Cookie"))
                    {
                        var cookies = response.Headers.GetValues("Set-Cookie");
                        await _debugService.LogAsync($"AuthService: Received {cookies.Count()} cookies from server");
                        
                        // Store cookies in localStorage to use in future requests
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authCookies", string.Join(";", cookies));
                        
                        // Apply cookies to document
                        foreach (var cookie in cookies)
                        {
                            var cookieParts = cookie.Split(';')[0].Split('=');
                            if (cookieParts.Length >= 2)
                            {
                                var cookieName = cookieParts[0].Trim();
                                var cookieValue = cookieParts[1].Trim();
                                await _debugService.LogAsync($"AuthService: Setting cookie: {cookieName}");
                                
                                // Store essential auth cookies for ABP
                                if (cookieName.Contains(".AspNetCore.Identity.Application"))
                                {
                                    await _jsRuntime.InvokeVoidAsync("eval", 
                                        $"document.cookie = '{cookieName}={cookieValue}; path=/; secure;'");
                                    await _debugService.LogAsync("AuthService: Set ABP identity cookie");
                                }
                            }
                        }
                    }
                    
                    // Store authentication state in localStorage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "isAuthenticated", "true");
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authTime", DateTime.UtcNow.ToString("o"));
                    
                    // Fetch user profile using the correct ABP endpoint from your API list
                    await _debugService.LogAsync("AuthService: Fetching user profile from ABP endpoint");
                    await FetchAndStoreCurrentUser();
                    
                    // Notify authentication state changed
                    await _debugService.LogAsync("AuthService: Notifying authentication state changed");
                    _authStateProvider.NotifyAuthenticationStateChanged();
                    
                    // Small delay to ensure UI updates
                    await Task.Delay(300); 
                    
                    await _debugService.LogAsync("AuthService: Login process completed successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _debugService.LogAsync($"AuthService: Login failed with status {response.StatusCode}: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"AuthService: Login exception: {ex.Message}, StackTrace: {ex.StackTrace}");
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
}