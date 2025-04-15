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

        builder.Services.AddScoped<HttpClient>(sp =>
            new HttpClient { BaseAddress = new Uri("https://localhost:44356/") });

        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
        });

        builder.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri("https://localhost:44356/");
        });

        var host = builder.Build();

        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        await jsRuntime.InvokeVoidAsync("eval", "import('https://cdn.jsdelivr.net/npm/hls.js@latest')");
        await jsRuntime.InvokeVoidAsync("eval", "import('/hlsPlayer.js')");

        await application.InitializeApplicationAsync(host.Services);
        await host.RunAsync();
    }
}
