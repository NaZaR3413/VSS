using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    // Custom HTTP message handler that adds the auth token to outgoing requests
    public class AuthorizationHeaderHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public AuthorizationHeaderHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if we have an authentication token
                bool isAuthenticated = await _jsRuntime.InvokeAsync<bool>("authTokenManager.isAuthenticated");
                
                if (isAuthenticated)
                {
                    var token = await _jsRuntime.InvokeAsync<string>("authTokenManager.getAccessToken");
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Add the authorization header with the token
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding auth header: {ex.Message}");
                // Continue with the request even if we couldn't add the auth header
            }

            // Continue with the request
            return await base.SendAsync(request, cancellationToken);
        }
    }
}