using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using web_backend.Blazor.Client.Services;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Web;

namespace web_backend.Blazor.Client;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Get API URL from configuration
        var configuration = builder.Configuration;
        var remoteServiceBaseUrl = configuration
            .GetSection("RemoteServices")
            .GetSection("Default")
            .GetValue<string>("BaseUrl");

        Console.WriteLine($"API URL from config: {remoteServiceBaseUrl}");

        // Configure HttpClient with the remote service URL and optimized settings
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress),
            DefaultRequestVersion = new Version(2, 0)  // Use HTTP/2 for better performance
        });

        // Enable prerendering support (works with server prerendering)
        builder.Services.AddOptions();
        builder.Services.AddAuthorizationCore();

        // Add memory caching for client-side caching
        builder.Services.AddMemoryCache();

        // Register Debug Service first
        builder.Services.AddSingleton<DebugService>();

        // Configure the application
        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
        });

        // Additional HttpClient for API with optimized settings
        builder.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress);
            client.DefaultRequestVersion = new Version(2, 0);
            
            // Set default timeout (adjust as needed)
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var host = builder.Build();

        // Try to get debug service and log startup
        var debugService = host.Services.GetService<DebugService>();
        if (debugService != null)
        {
            await debugService.Initialize();
            await debugService.LogAsync("Host built, starting application");
        }

        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        await jsRuntime.InvokeVoidAsync("console.log", "About to initialize application...");
        
        try
        {
            await application.InitializeApplicationAsync(host.Services);
            await jsRuntime.InvokeVoidAsync("console.log", "Application initialized successfully");
        }
        catch (Exception ex)
        {
            await jsRuntime.InvokeVoidAsync("console.error", $"Error initializing application: {ex.Message}");
        }

        try
        {
            await jsRuntime.InvokeVoidAsync("console.log", "Running host...");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            await jsRuntime.InvokeVoidAsync("console.error", $"Error running host: {ex.Message}");
        }
    }
}

