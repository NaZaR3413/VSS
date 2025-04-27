using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client.Services
{
    /// <summary>
    /// Service to handle CORS and HTTP request interception
    /// </summary>
    public class CorsInterceptorService
    {
        private readonly ILogger<CorsInterceptorService> _logger;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        public CorsInterceptorService(
            ILogger<CorsInterceptorService> logger,
            IJSRuntime jsRuntime,
            NavigationManager navigationManager)
        {
            _logger = logger;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        /// <summary>
        /// Initializes the JavaScript CORS interceptor
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing CORS interceptor service");
                // JS initialization is done automatically when the script loads
                await _jsRuntime.InvokeVoidAsync("console.log", "CORS interceptor service initialized from C#");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing CORS interceptor");
            }
        }

        /// <summary>
        /// Adds CORS headers to an HttpClient instance
        /// </summary>
        public void ConfigureHttpClient(HttpClient httpClient, string baseUrl = null)
        {
            _logger.LogInformation("Configuring HttpClient with CORS headers");

            // Set base address if provided
            if (!string.IsNullOrEmpty(baseUrl))
            {
                httpClient.BaseAddress = new Uri(baseUrl);
            }

            // Set default headers that help with CORS
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            
            // Set credentials mode
            // Note: this is handled by the browser, we just document it here
            // httpClient would use 'credentials: include' in the fetch API

            _logger.LogInformation("HttpClient configured with CORS headers");
        }

        /// <summary>
        /// Creates a message handler that adds CORS headers to requests
        /// </summary>
        public DelegatingHandler CreateCorsMessageHandler()
        {
            return new CorsHeaderHandler(_logger);
        }

        /// <summary>
        /// Custom HTTP message handler that adds CORS headers to requests
        /// </summary>
        private class CorsHeaderHandler : DelegatingHandler
        {
            private readonly ILogger _logger;

            public CorsHeaderHandler(ILogger logger)
            {
                _logger = logger;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                try
                {
                    // Add CORS-related headers
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    
                    // Ensure we have Accept header
                    if (request.Headers.Accept.Count == 0)
                    {
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying CORS headers to request");
                }

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}