using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly IConfiguration _configuration;
        private readonly DebugService _debugService;
        
        public AuthService(
            HttpClient httpClient, 
            IJSRuntime jsRuntime, 
            AuthenticationStateProvider authStateProvider,
            IConfiguration configuration,
            DebugService debugService)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _authStateProvider = (CustomAuthStateProvider)authStateProvider;
            _configuration = configuration;
            _debugService = debugService;
        }
        
        public async Task<bool> CheckAuthenticationStatus()
        {
            try
            {
                await _debugService.LogAsync("Checking authentication status");
                
                // Try to get current user from API
                var apiBaseUrl = _configuration
                    .GetSection("RemoteServices")
                    .GetSection("Default")
                    .GetValue<string>("BaseUrl") ?? "";
                
                var userInfoEndpoint = $"{apiBaseUrl}/api/account/my-profile";
                
                // Configure request to include credentials
                var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
                
                // Get user profile
                var response = await _httpClient.SendAsync(request);
                
                // Log response for debugging
                await _debugService.LogAsync($"Auth check response: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    await _debugService.LogAsync("User not authenticated");
                    await _authStateProvider.ClearAuthenticationState();
                    return false;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                await _debugService.LogAsync($"User profile: {responseContent}");
                
                // Parse user info
                var userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (userInfo == null)
                {
                    await _debugService.LogAsync("Failed to parse user info");
                    return false;
                }
                
                // Set authenticated user in state provider
                await _authStateProvider.SetAuthenticatedUser(userInfo);
                await _debugService.LogAsync($"User authenticated: {userInfo.UserName}");
                return true;
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"Error checking auth status: {ex.Message}");
                return false;
            }
        }
        
        public async Task Logout()
        {
            try
            {
                await _debugService.LogAsync("Logging out");
                
                // Clear authentication state
                await _authStateProvider.ClearAuthenticationState();
                
                // Redirect to backend logout endpoint
                var apiBaseUrl = _configuration
                    .GetSection("RemoteServices")
                    .GetSection("Default")
                    .GetValue<string>("BaseUrl") ?? "";
                
                var logoutUrl = $"{apiBaseUrl}/Account/Logout";
                await _jsRuntime.InvokeVoidAsync("window.location.replace", logoutUrl);
            }
            catch (Exception ex)
            {
                await _debugService.LogAsync($"Error during logout: {ex.Message}");
            }
        }
    }
    
    // Extension method for HttpRequestMessage to enable credentials
    public static class HttpRequestMessageExtensions
    {
        public static void SetBrowserRequestCredentials(this HttpRequestMessage request, BrowserRequestCredentials credentials)
        {
            request.SetBrowserRequestCache(BrowserRequestCache.NoCache);
            request.Headers.Add("X-BrowserRequestCredentials", credentials.ToString().ToLowerInvariant());
        }
        
        public static void SetBrowserRequestCache(this HttpRequestMessage request, BrowserRequestCache cache)
        {
            request.Headers.Add("X-BrowserRequestCache", cache.ToString().ToLowerInvariant());
        }
    }
    
    public enum BrowserRequestCredentials
    {
        Omit,
        SameOrigin,
        Include
    }
    
    public enum BrowserRequestCache
    {
        Default,
        NoStore,
        Reload,
        NoCache,
        ForceCache,
        OnlyIfCached
    }
}