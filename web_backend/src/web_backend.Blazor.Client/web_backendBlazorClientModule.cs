using System;
using System.Linq;
using System.Net.Http;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using web_backend.Blazor.Client.Menus;
using web_backend.Blazor.Client.Services;
using OpenIddict.Abstractions;
using Volo.Abp.AspNetCore.Components.Web.Theming.Routing;
using Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme;
using Volo.Abp.Autofac.WebAssembly;
using Volo.Abp.AutoMapper;
using Volo.Abp.Http.Client;
using Volo.Abp.Identity.Blazor.WebAssembly;
using Volo.Abp.IdentityModel;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.Blazor.WebAssembly;
using Volo.Abp.TenantManagement.Blazor.WebAssembly;
using Volo.Abp.UI.Navigation;
using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using web_backend.Localization;

namespace web_backend.Blazor.Client;

[DependsOn(
    typeof(AbpAutofacWebAssemblyModule),
    typeof(web_backendHttpApiClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyBasicThemeModule),
    typeof(AbpIdentityBlazorWebAssemblyModule),
    typeof(AbpTenantManagementBlazorWebAssemblyModule),
    typeof(AbpSettingManagementBlazorWebAssemblyModule)
)]
public class web_backendBlazorClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();

        // Skip explicit root component registration - we'll let Program.cs handle this
        // ConfigureRootComponents(builder);
        
        ConfigureAuthentication(builder);
        ConfigureHttpClient(context, environment);
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);
        ConfigureAutoMapper(context);
        ConfigureLocalization(context);
        
        // Add LivestreamStateService for real-time updates
        context.Services.AddSingleton<LivestreamStateService>();
    }

    // We're commenting out this method to avoid duplicate root component registration
    // private void ConfigureRootComponents(WebAssemblyHostBuilder builder)
    // {
    //     builder.RootComponents.Add<App>("#app");
    // }

    private void ConfigureRouter(ServiceConfigurationContext context)
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(web_backendBlazorClientModule).Assembly;
        });
    }

    private void ConfigureMenu(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new web_backendMenuContributor(context.Services.GetConfiguration()));
        });
    }

    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        context.Services
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
    }

    private void ConfigureLocalization(ServiceConfigurationContext context)
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<web_backendResource>()
                .AddBaseTypes(typeof(AbpUiResource));
                
            // Make sure AbpUi is registered as a resource
            var hasAbpUiResource = options.Resources.Any(r => r.Value.GetType() == typeof(AbpUiResource) || 
                                                           r.Key == "AbpUi"); // Use the string name instead of a non-existent property
            if (!hasAbpUiResource)
            {
                options.Resources.Add<AbpUiResource>();
            }
        });
    }

    private static void ConfigureAuthentication(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("AuthServer", options.ProviderOptions);
            options.UserOptions.NameClaim = OpenIddictConstants.Claims.Name;
            options.UserOptions.RoleClaim = OpenIddictConstants.Claims.Role;

            options.ProviderOptions.DefaultScopes.Add("web_backend");
            options.ProviderOptions.DefaultScopes.Add("roles");
            options.ProviderOptions.DefaultScopes.Add("email");
            options.ProviderOptions.DefaultScopes.Add("phone");
            
            // Set these explicitly based on server configuration
            options.ProviderOptions.ResponseType = "code";
            
            // Use the same client ID from your appsettings.json
            options.ProviderOptions.ClientId = "web_backend_Blazor";
            
            var configuration = builder.Services.GetSingletonInstance<IConfiguration>();
            var remoteServiceBaseUrl = configuration
                .GetSection("RemoteServices")
                .GetSection("Default")
                .GetValue<string>("BaseUrl");
            
            if (!string.IsNullOrEmpty(remoteServiceBaseUrl))
            {
                // These URLs must match what OpenIddict expects on the server
                options.ProviderOptions.Authority = remoteServiceBaseUrl;
                // Set explicit metadata URL to ensure discovery works
                options.ProviderOptions.MetadataUrl = $"{remoteServiceBaseUrl}/.well-known/openid-configuration";
                
                // Add logging to help diagnose issues
                Console.WriteLine($"Auth Server Base URL: {remoteServiceBaseUrl}");
                Console.WriteLine($"Metadata URL: {options.ProviderOptions.MetadataUrl}");
            }
            
            // Set callback path
            options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
            options.ProviderOptions.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;
            
            // Add better error handling
            options.ProviderOptions.ResponseMode = "query";
        });
    }

    private static void ConfigureHttpClient(ServiceConfigurationContext context, IWebAssemblyHostEnvironment environment)
    {
        var configuration = context.Services.GetSingletonInstance<IConfiguration>();
        var remoteServiceBaseUrl = configuration
            .GetSection("RemoteServices")
            .GetSection("Default")
            .GetValue<string>("BaseUrl");

        if (string.IsNullOrEmpty(remoteServiceBaseUrl))
        {
            remoteServiceBaseUrl = environment.BaseAddress;
        }

        Console.WriteLine($"Using API base address: {remoteServiceBaseUrl}");

        // Add handler for diagnostic and connection issues
        var handler = new HttpClientHandler();
        
        // Allow untrusted certificates in development for testing
        if (environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Console.WriteLine("Warning: SSL certificate validation is disabled in development mode");
        }

        context.Services.AddTransient(sp => new HttpClient(handler)
        {
            BaseAddress = new Uri(remoteServiceBaseUrl),
            Timeout = TimeSpan.FromSeconds(30) // Increase timeout for better diagnostics
        });
        
        // Register the AbpMvcClient explicitly
        context.Services.AddHttpClient("AbpMvcClient", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        
        // Register API client
        context.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        
        // Disable the IdentityModelAuthenticationService that's trying to use authorization_code
        context.Services.RemoveAll<IIdentityModelAuthenticationService>();
        context.Services.AddSingleton<IIdentityModelAuthenticationService, NullIdentityModelAuthenticationService>();
    }

    private void ConfigureAutoMapper(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<web_backendBlazorClientModule>();
        });
    }
}
