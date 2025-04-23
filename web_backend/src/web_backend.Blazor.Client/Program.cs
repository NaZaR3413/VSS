using System;
using System.Net.Http;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using web_backend.Blazor.Client.Services;

namespace web_backend.Blazor.Client;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Get API URL from configuration
        var configuration = builder.Configuration;
        var remoteServiceBaseUrl = configuration
            .GetSection("RemoteServices")
            .GetSection("Default")
            .GetValue<string>("BaseUrl");

        Console.WriteLine($"API URL from config: {remoteServiceBaseUrl}");

        // Configure HttpClient with the remote service URL
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress)
        });

        // Configure the application
        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
        });

        // Additional HttpClient for API
        builder.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress);
        });

        var host = builder.Build();

        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        await jsRuntime.InvokeVoidAsync("eval", "import('https://cdn.jsdelivr.net/npm/hls.js@latest')");
        await jsRuntime.InvokeVoidAsync("eval", "import('/hlsPlayer.js')");

        await application.InitializeApplicationAsync(host.Services);
        await host.RunAsync();
    }
}

