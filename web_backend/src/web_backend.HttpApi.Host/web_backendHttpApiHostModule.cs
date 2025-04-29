using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using web_backend.EntityFrameworkCore;
using web_backend.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.OpenIddict;
using System.Security.Cryptography.X509Certificates;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.CookiePolicy;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;


namespace web_backend
{
    [DependsOn(
        typeof(web_backendHttpApiModule),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMultiTenancyModule),
        typeof(web_backendApplicationModule),
        typeof(web_backendEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreMvcUiBasicThemeModule),
        typeof(AbpAccountWebOpenIddictModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpSwashbuckleModule)
    )]
    public class web_backendHttpApiHostModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            Console.WriteLine($"Hosting Environment: {hostingEnvironment.EnvironmentName}");

            if (!hostingEnvironment.IsDevelopment())
            {
                Console.WriteLine("Running production OpenIddict configuration...");

                PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
                {
                    options.AddDevelopmentEncryptionAndSigningCertificate = false;
                });

                PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
                {
                    try
                    {
                        var certPath = Path.Combine(AppContext.BaseDirectory, "openiddict.pfx");

                        if (!File.Exists(certPath))
                        {
                            Console.WriteLine("Certificate not found. Using development certificate.");
                            PreConfigure<AbpOpenIddictAspNetCoreOptions>(f => f.AddDevelopmentEncryptionAndSigningCertificate = true);
                            return;
                        }

                        var certificate = new X509Certificate2(
                            certPath,
                            "Varsity2024",
                            X509KeyStorageFlags.MachineKeySet);

                        serverBuilder.AddEncryptionCertificate(certificate);
                        serverBuilder.AddSigningCertificate(certificate);
                    }
                    catch
                    {
                        PreConfigure<AbpOpenIddictAspNetCoreOptions>(f => f.AddDevelopmentEncryptionAndSigningCertificate = true);
                    }
                });
            }
            else
            {
                PreConfigure<AbpOpenIddictAspNetCoreOptions>(o =>
                {
                    o.AddDevelopmentEncryptionAndSigningCertificate = true;
                });
            }

            PreConfigure<OpenIddictBuilder>(b =>
            {
                b.AddValidation(o =>
                {
                    o.AddAudiences("web_backend");
                    o.UseLocalServer();
                    o.UseAspNetCore();
                });
            });

            ObjectExtensionManager.Instance.MapEfCoreProperty<AbpIdentityUser, string>(
    "Name",
    (eb, pb) => pb.HasMaxLength(IdentityUserConsts.MaxUserNameLength));

ObjectExtensionManager.Instance.MapEfCoreProperty<AbpIdentityUser, string>(
    "PhoneNumber",
    (eb, pb) => pb.HasMaxLength(IdentityUserConsts.MaxPhoneNumberLength));

        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            ConfigureAuthentication(context);
            ConfigureBundles();
            ConfigureUrls(configuration);
            ConfigureConventionalControllers();
            ConfigureVirtualFileSystem(context);
            ConfigureCors(context, configuration);
            ConfigureSwaggerServices(context, configuration);
            ConfigureHttpClients(context);

        }

        private void ConfigureAuthentication(ServiceConfigurationContext context)
        {
            context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(o => o.IsDynamicClaimsEnabled = true);

            // Cross-origin-friendly auth cookie
            context.Services.Configure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme,
                opts =>
                {
                    opts.Cookie.SameSite = SameSiteMode.None;
                    opts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });
        }

        private void ConfigureBundles()
        {
            Configure<AbpBundlingOptions>(o =>
            {
                o.StyleBundles.Configure(
                    BasicThemeBundles.Styles.Global,
                    b => b.AddFiles("/global-styles.css"));
            });
        }

        private void ConfigureUrls(IConfiguration configuration)
        {
            Configure<AppUrlOptions>(o =>
            {
                o.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
                o.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
                o.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
                o.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
            });
        }

        private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
        {
            var env = context.Services.GetHostingEnvironment();
            if (env.IsDevelopment())
            {
                Configure<AbpVirtualFileSystemOptions>(o =>
                {
                    o.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainSharedModule>(Path.Combine(env.ContentRootPath, "..", "web_backend.Domain.Shared"));
                    o.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainModule>(Path.Combine(env.ContentRootPath, "..", "web_backend.Domain"));
                    o.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationContractsModule>(Path.Combine(env.ContentRootPath, "..", "web_backend.Application.Contracts"));
                    o.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationModule>(Path.Combine(env.ContentRootPath, "..", "web_backend.Application"));
                });
            }
        }

        private void ConfigureHttpClients(ServiceConfigurationContext context)
        {
            context.Services.AddHttpClient("StreamProxy", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
        }


        private void ConfigureConventionalControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(o =>
            {
                o.ConventionalControllers.Create(typeof(web_backendApplicationModule).Assembly);
            });
        }

        private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddAbpSwaggerGenWithOAuth(
                configuration["AuthServer:Authority"]!,
                new Dictionary<string, string> { { "web_backend", "web_backend API" } },
                o =>
                {
                    o.SwaggerDoc("v1", new OpenApiInfo { Title = "web_backend API", Version = "v1" });
                    o.DocInclusionPredicate((_, __) => true);
                    o.CustomSchemaIds(t => t.FullName);
                });
        }

        private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddCors(o =>
            {
                o.AddDefaultPolicy(b =>
                {
                    var corsOrigins = configuration["App:CorsOrigins"]?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(origin => origin.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>();

                    var env = context.Services.GetHostingEnvironment();
                    if (env.IsDevelopment())
                    {
                        corsOrigins = corsOrigins.Append("*").ToArray();
                    }

                    b.WithOrigins(corsOrigins)
                     .WithAbpExposedHeaders()
                     .SetIsOriginAllowedToAllowWildcardSubdomains()
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
                });
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async ctx =>
                    {
                        ctx.Response.StatusCode = 500;
                        ctx.Response.ContentType = "text/html";
                        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                        if (ex != null)
                        {
                            var logger = ctx.RequestServices.GetService<ILogger<web_backendHttpApiHostModule>>();
                            logger?.LogError(ex, "Unhandled exception");
                            await ctx.Response.WriteAsync($"<h1>{ex.Message}</h1><pre>{ex.StackTrace}</pre>");
                        }
                    });
                });
            }

            app.UseAbpRequestLocalization();
            app.UseCorrelationId();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            // cookie policy for cross-origin requests
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();
            app.UseAbpOpenIddictValidation();

            if (MultiTenancyConsts.IsEnabled)
            {
                app.UseMultiTenancy();
            }

            app.UseUnitOfWork();
            app.UseDynamicClaims();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseAbpSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "web_backend API");
                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                c.OAuthScopes("web_backend");
            });

            app.UseAuditing();
            app.UseAbpSerilogEnrichers();
            app.UseConfiguredEndpoints();
        }
    }
}
