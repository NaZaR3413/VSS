using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using web_backend.Blazor.Client.Services;

namespace web_backend.Blazor.Client;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        
        builder.RootComponents.Add<App>("#app");

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

        // Register Debug Service first
        builder.Services.AddSingleton<DebugService>();

        // Configure the application with error handling
        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
        });

        // Additional HttpClient for API with proper configuration
        builder.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress);
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
            if (ex.InnerException != null)
            {
                await jsRuntime.InvokeVoidAsync("console.error", $"Inner exception: {ex.InnerException.Message}");
            }
            
            if (debugService != null)
            {
                await debugService.LogAsync($"Error initializing application: {ex.Message}");
            }
        }

        try
        {
            await jsRuntime.InvokeVoidAsync("console.log", "Running host...");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            await jsRuntime.InvokeVoidAsync("console.error", $"Error running host: {ex.Message}");
            if (ex.InnerException != null)
            {
                await jsRuntime.InvokeVoidAsync("console.error", $"Inner exception: {ex.InnerException.Message}");
            }
            
            if (debugService != null)
            {
                await debugService.LogAsync($"Error running host: {ex.Message}");
            }
        }
    }
}

