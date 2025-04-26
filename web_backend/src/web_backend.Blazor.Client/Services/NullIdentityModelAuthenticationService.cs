using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.IdentityModel;

namespace web_backend.Blazor.Client.Services
{
    /// <summary>
    /// A null implementation of IIdentityModelAuthenticationService that doesn't attempt
    /// to use the unsupported grant types that are causing errors
    /// </summary>
    public class NullIdentityModelAuthenticationService : IIdentityModelAuthenticationService, ISingletonDependency
    {
        private readonly ILogger<NullIdentityModelAuthenticationService> _logger;

        public NullIdentityModelAuthenticationService(ILogger<NullIdentityModelAuthenticationService> logger)
        {
            _logger = logger;
        }

        public Task<string> GetAccessTokenAsync(IdentityClientConfiguration configuration)
        {
            // Log that we're bypassing token acquisition
            _logger.LogInformation("Bypassing access token acquisition");
            
            // Return empty token - authentication will be handled by OIDC in Blazor WebAssembly
            return Task.FromResult(string.Empty);
        }
        
        public Task<bool> TryAuthenticateAsync(HttpClient client, string? clientName = null)
        {
            // Log that we're bypassing authentication
            _logger.LogInformation($"Bypassing authentication for client: {clientName ?? "default"}");
            
            // Add a dummy Authorization header to prevent subsequent auth attempts
            if (!client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Add("X-Auth-Status", "bypassed");
            }
            
            // Consider authentication successful to prevent blocking
            return Task.FromResult(true);
        }
    }
}