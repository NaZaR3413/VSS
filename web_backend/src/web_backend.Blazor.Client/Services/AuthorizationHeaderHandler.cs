using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    // Custom HTTP handler that adds authentication headers to outgoing requests
    public class AuthorizationHeaderHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly DebugService _debugService;

        public AuthorizationHeaderHandler(IJSRuntime jsRuntime, DebugService debugService)
        {
            _jsRuntime = jsRuntime;
            _debugService = debugService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add AJAX header to prevent redirects to login page
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            
            // Add Authentication header if we're authenticated
            var isAuthenticated = await _jsRuntime.InvokeAsync<bool>("eval", 
                "localStorage.getItem('isAuthenticated') === 'true'");
            
            if (isAuthenticated)
            {
                // Check for access token first (if using JWT auth)
                var token = await _jsRuntime.InvokeAsync<string>("eval", 
                    "localStorage.getItem('accessToken') || ''");
                
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    await _debugService.LogAsync($"Added Bearer token to request: {request.RequestUri}");
                }
                
                // Add Accept header for JSON responses
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            
            // Send request to the server
            var response = await base.SendAsync(request, cancellationToken);
            
            // If we get a 401/403 response, mark as not authenticated
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "isAuthenticated");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authTime");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
                
                await _debugService.LogAsync($"Cleared auth data due to {response.StatusCode} response from {request.RequestUri}");
            }
            
            return response;
        }
    }
}