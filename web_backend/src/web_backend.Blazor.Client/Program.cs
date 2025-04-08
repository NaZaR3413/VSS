using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace web_backend.Blazor.Client;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Set API base URL (Ensure this is before AddApplicationAsync)
        builder.Services.AddScoped<HttpClient>(sp =>
            new HttpClient { BaseAddress = new Uri("https://localhost:44356/") });

        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
        });

        var host = builder.Build();

        // Load JavaScript files when the Blazor app starts
        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        await jsRuntime.InvokeVoidAsync("eval", "import('https://cdn.jsdelivr.net/npm/hls.js@latest')");
        await jsRuntime.InvokeVoidAsync("eval", "import('/hlsPlayer.js')");

        await application.InitializeApplicationAsync(host.Services);
        await host.RunAsync();
    }
}
