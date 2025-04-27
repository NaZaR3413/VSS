using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace web_backend.Blazor.Client.Services
{
    public class HttpClientAuthorizationDelegatingHandler : DelegatingHandler, ITransientDependency
    {
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly ILogger<HttpClientAuthorizationDelegatingHandler> _logger;

        public HttpClientAuthorizationDelegatingHandler(
            IAccessTokenProvider tokenProvider,
            ILogger<HttpClientAuthorizationDelegatingHandler> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Try to get the token
            var tokenResult = await _tokenProvider.RequestAccessToken();

            if (tokenResult.TryGetToken(out var token))
            {
                _logger.LogDebug("Adding bearer token to request");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            }
            else
            {
                _logger.LogWarning("Unable to get access token for API request");
            }

            // Add CORS headers that may help with cross-origin requests
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}