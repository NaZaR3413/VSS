using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        public Task<string> GetAccessTokenAsync(IdentityClientConfiguration configuration)
        {
            // Return empty token - authentication will be handled by OIDC in Blazor WebAssembly
            return Task.FromResult(string.Empty);
        }
        
        public Task<bool> TryAuthenticateAsync(HttpClient client, string? clientName = null)
        {
            // No authentication needed at this level, handled by OIDC
            return Task.FromResult(true);
        }
    }
}