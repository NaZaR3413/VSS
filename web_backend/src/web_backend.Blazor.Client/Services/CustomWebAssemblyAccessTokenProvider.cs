using System.Threading.Tasks;
using Microsoft.JSInterop;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Http.Client.Authentication;

namespace web_backend.Blazor.Client.Services
{
    public class CustomWebAssemblyAccessTokenProvider : IRemoteServiceHttpClientAuthenticator, ITransientDependency
    {
        private readonly IJSRuntime _jsRuntime;

        public CustomWebAssemblyAccessTokenProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        
        public async Task Authenticate(RemoteServiceHttpClientAuthenticateContext context)
        {
            string token = await GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Get the token from local storage using the same method your auth service uses
                return await _jsRuntime.InvokeAsync<string>("authTokenManager.getToken");
            }
            catch
            {
                return null;
            }
        }
    }
}