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
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

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

        // Register our custom authentication state provider
        builder.Services.AddScoped<TokenAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
            provider.GetRequiredService<TokenAuthenticationStateProvider>());
        
        // Add authorization core services
        builder.Services.AddAuthorizationCore();

        // Register our custom HTTP message handler
        builder.Services.AddScoped<AuthorizationHeaderHandler>();

        // Configure HttpClient with the token authorization handler
        builder.Services.AddScoped(sp => {
            var authHandler = sp.GetRequiredService<AuthorizationHeaderHandler>();
            authHandler.InnerHandler = new HttpClientHandler();
            
            return new HttpClient(authHandler) {
                BaseAddress = new Uri(remoteServiceBaseUrl ?? builder.HostEnvironment.BaseAddress),
                DefaultRequestVersion = new Version(2, 0)  // Use HTTP/2 for better performance
            };
        });

        // Enable memory caching for client-side caching
        builder.Services.AddMemoryCache();

        // Register Debug Service first
        builder.Services.AddSingleton<DebugService>();

        // Configure the application
        var application = await builder.AddApplicationAsync<web_backendBlazorClientModule>(options =>
        {
            options.UseAutofac();
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

