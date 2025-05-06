using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Components.WebAssembly;

namespace web_backend.Blazor.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var apiBase = builder.Configuration["RemoteServices:Default:BaseUrl"]
                          ?? builder.HostEnvironment.BaseAddress;

            // default & named HttpClient
            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase) });
            builder.Services.AddHttpClient("API", c => c.BaseAddress = new Uri(apiBase));

            // ABP module
            var abpApp = await builder.AddApplicationAsync<web_backendBlazorClientModule>(o => o.UseAutofac());

            var host = builder.Build();

            // initialize and run
            await abpApp.InitializeApplicationAsync(host.Services);
            await host.RunAsync();
        }
    }
}
