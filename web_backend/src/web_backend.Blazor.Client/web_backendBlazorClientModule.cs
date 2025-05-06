using System;
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
using Volo.Abp.Http.Client.IdentityModel.WebAssembly;
using Volo.Abp.AspNetCore.Components.WebAssembly;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Volo.Abp.AspNetCore.Components.Web;

namespace web_backend.Blazor.Client;

[DependsOn(
    typeof(AbpAutofacWebAssemblyModule),
    typeof(web_backendHttpApiClientModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyBasicThemeModule),
    typeof(AbpIdentityBlazorWebAssemblyModule),
    typeof(AbpTenantManagementBlazorWebAssemblyModule),
    typeof(AbpSettingManagementBlazorWebAssemblyModule),
    typeof(AbpHttpClientIdentityModelWebAssemblyModule)
)]
public class web_backendBlazorClientModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // Skip setting application configuration path at this level
        // We'll define it directly in the appsettings.json file instead
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();

        ConfigureAuthentication(builder);
        ConfigureHttpClient(context, environment);
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);
        ConfigureAutoMapper(context);

        // Add LivestreamStateService for real-time updates
        context.Services.AddSingleton<LivestreamStateService>();
    }

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

            // Ensure these are set correctly
            options.ProviderOptions.DefaultScopes.Add("web_backend");
            options.ProviderOptions.DefaultScopes.Add("roles");
            options.ProviderOptions.DefaultScopes.Add("email");
            options.ProviderOptions.DefaultScopes.Add("phone");
            options.ProviderOptions.DefaultScopes.Add("profile");

            // Make sure redirect URIs are properly set as relative paths
            options.ProviderOptions.RedirectUri = "authentication/login-callback";
            options.ProviderOptions.PostLogoutRedirectUri = "authentication/logout-callback";

            // Ensure response type is set correctly
            options.ProviderOptions.ResponseType = "code";
        });
    }

    private void ConfigureHttpClient(ServiceConfigurationContext context, IWebAssemblyHostEnvironment environment)
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

        // Configure the default HttpClient
        context.Services.AddTransient(sp => new HttpClient
        {
            BaseAddress = new Uri(remoteServiceBaseUrl)
        });

        // Configure AbpRemoteServiceOptions with proper API settings
        Configure<AbpRemoteServiceOptions>(options =>
        {
            options.RemoteServices.Default = new RemoteServiceConfiguration(remoteServiceBaseUrl);
        });

        // Configure the ABP HTTP clients
        context.Services.AddHttpClientProxies(
            typeof(web_backendHttpApiClientModule).Assembly,
            remoteServiceBaseUrl
        );

        // Configure HTTP client settings
        Configure<AbpHttpClientOptions>(options =>
        {
            // Configure HTTP client options here if needed
        });

        // Configure the named HttpClients
        context.Services.AddHttpClient("Default", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        context.Services.AddHttpClient("AbpMvcClient", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        context.Services.AddHttpClient("API", client =>
        {
            client.BaseAddress = new Uri(remoteServiceBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
    }

    private void ConfigureAutoMapper(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<web_backendBlazorClientModule>();
        });
    }
}
