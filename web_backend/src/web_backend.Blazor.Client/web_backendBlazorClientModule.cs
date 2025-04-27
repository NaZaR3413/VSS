using System;
using System.Net.Http;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
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
        
        // Add LivestreamStateService for real-time updates
        context.Services.AddSingleton<LivestreamStateService>();
        
        // Add DebugService for improved error handling and logging
        context.Services.AddSingleton<DebugService>();

        // Register custom authentication provider and service
        context.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        context.Services.AddScoped<AuthService>();

        // Configure HTTP client to include cookies for all requests
        context.Services.Configure<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.PrimaryHandler = new HttpClientHandler
                {
                    UseCookies = true,
                    AllowAutoRedirect = false,
                    UseDefaultCredentials = false
                };
            });
        });
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
            }
            
            // Set callback path
            options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
            options.ProviderOptions.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;
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

        context.Services.AddTransient(sp => new HttpClient
        {
            BaseAddress = new Uri(remoteServiceBaseUrl)
        });
        
        // Register the AbpMvcClient explicitly
        context.Services.AddHttpClient("AbpMvcClient", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
        });
        
        // Register API client
        context.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
        });
        
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
