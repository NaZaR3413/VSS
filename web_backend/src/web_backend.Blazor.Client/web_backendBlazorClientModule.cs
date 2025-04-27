using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blazorise.Bootstrap5;
using Blazorise;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using web_backend.Blazor.Client.Menus;
using web_backend.Blazor.Client.Services;
using OpenIddict.Abstractions;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Components.Web.Theming.Routing;
using Volo.Abp.AspNetCore.Components.Web.Theming.Toolbars;
using Volo.Abp.AspNetCore.Components.WebAssembly.Theming;
using Volo.Abp.Autofac.WebAssembly;
using Volo.Abp.AutoMapper;
using Volo.Abp.Http.Client;
using Volo.Abp.Http.Client.Authentication;
using Volo.Abp.Http.Client.IdentityModel.WebAssembly;
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
    typeof(AbpAspNetCoreComponentsWebAssemblyThemingModule),
    typeof(AbpAccountHttpApiClientModule),
    typeof(AbpIdentityBlazorWebAssemblyModule),
    typeof(AbpTenantManagementBlazorWebAssemblyModule),
    typeof(AbpSettingManagementBlazorWebAssemblyModule),
    typeof(AbpHttpClientIdentityModelWebAssemblyModule)
)]
public class web_backendBlazorClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();

        ConfigureAuthentication(builder);
        ConfigureHttpClient(context, environment);
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureUI(builder);
        ConfigureMenu(context);
        ConfigureAutoMapper(context);
        
        // Register our custom services
        ConfigureCustomServices(context);
    }
    
    private void ConfigureCustomServices(ServiceConfigurationContext context)
    {
        // Register CORS interceptor service
        context.Services.AddSingleton<CorsInterceptorService>();
        
        // Register debug service
        context.Services.AddSingleton<DebugService>();
        
        // Register video service
        context.Services.AddSingleton<VideoService>();
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

        Configure<AbpToolbarOptions>(options =>
        {
            options.Contributors.Add(new web_backendToolbarContributor());
        });
    }

    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        context.Services
            .AddBlazorise()
            .AddBootstrap5Providers();
    }

    private void ConfigureAuthentication(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("AuthServer", options.ProviderOptions);
            options.UserOptions.RoleClaim = "role";
            
            // Add JWT header configuration
            // This ensures that the Authorization header is added to API requests
            options.ProviderOptions.DefaultScopes.Add("web_backend");
            options.ProviderOptions.ResponseType = "code";
        });
    }

    private static void ConfigureHttpClient(ServiceConfigurationContext context, IWebAssemblyHostEnvironment environment)
    {
        context.Services.AddTransient<HttpClientAuthorizationDelegatingHandler>();
        context.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(environment.BaseAddress)
        });
        
        // Configure HttpClient with CORS handling
        context.Services.AddTransient(sp => {
            // Create a message handler that adds CORS headers
            var corsService = sp.GetRequiredService<CorsInterceptorService>();
            var corsHandler = corsService.CreateCorsMessageHandler();
            
            // Create and configure the HttpClient
            var httpClient = new HttpClient(corsHandler)
            {
                BaseAddress = new Uri(environment.BaseAddress)
            };
            
            // Add default headers
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            
            return httpClient;
        });

        context.Services.AddHttpClient("web_backend.HttpApi.Client", client =>
            {
                client.BaseAddress = new Uri(environment.BaseAddress);
            })
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>();
    }

    private static void ConfigureUI(WebAssemblyHostBuilder builder)
    {
        builder.RootComponents.Add<App>("#ApplicationContainer");
    }

    private void ConfigureAutoMapper(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<web_backendBlazorClientModule>();
        });
    }
}
