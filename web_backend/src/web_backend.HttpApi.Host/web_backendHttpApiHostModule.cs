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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.CookiePolicy;
using AbpIdentityUser = Volo.Abp.Identity.IdentityUser;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

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
        typeof(AbpSwashbuckleModule),
        typeof(AbpIdentityHttpApiModule),
        typeof(AbpBlobStoringAzureModule)             // Azure Blob Storage
    )]
    public class web_backendHttpApiHostModule : AbpModule
    {
        /* ----------------------------------------------------------
           PRE‑CONFIGURE
        -----------------------------------------------------------*/
        public override void PreConfigureServices(ServiceConfigurationContext ctx)
        {
            var env = ctx.Services.GetHostingEnvironment();

            if (!env.IsDevelopment())
            {
                PreConfigure<AbpOpenIddictAspNetCoreOptions>(o =>
                    o.AddDevelopmentEncryptionAndSigningCertificate = false);

                PreConfigure<OpenIddictServerBuilder>(builder =>
                {
                    var certPath = Path.Combine(AppContext.BaseDirectory, "openiddict.pfx");
                    if (File.Exists(certPath))
                    {
                        var cert = new X509Certificate2(
                            certPath, "Varsity2024", X509KeyStorageFlags.MachineKeySet);
                        builder.AddEncryptionCertificate(cert);
                        builder.AddSigningCertificate(cert);
                    }
                    else
                    {
                        PreConfigure<AbpOpenIddictAspNetCoreOptions>(f =>
                            f.AddDevelopmentEncryptionAndSigningCertificate = true);
                    }
                });
            }
            else
            {
                PreConfigure<AbpOpenIddictAspNetCoreOptions>(o =>
                    o.AddDevelopmentEncryptionAndSigningCertificate = true);
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

            /* extend Identity user */
            ObjectExtensionManager.Instance.MapEfCoreProperty<AbpIdentityUser, string>(
                "Name", (e, p) => p.HasMaxLength(IdentityUserConsts.MaxUserNameLength));
            ObjectExtensionManager.Instance.MapEfCoreProperty<AbpIdentityUser, string>(
                "PhoneNumber", (e, p) => p.HasMaxLength(IdentityUserConsts.MaxPhoneNumberLength));
        }

        /* ----------------------------------------------------------
           CONFIGURE
        -----------------------------------------------------------*/
        public override void ConfigureServices(ServiceConfigurationContext ctx)
        {
            var cfg = ctx.Services.GetConfiguration();

            ConfigureAuthentication(ctx);
            ConfigureBundles();
            ConfigureUrls(cfg);
            ConfigureConventionalControllers();
            ConfigureVirtualFileSystem(ctx);
            ConfigureCors(ctx, cfg);
            ConfigureSwaggerServices(ctx, cfg);
            ConfigureHttpClients(ctx);
            ConfigureAzureBlobStorage(ctx, cfg);          //  <- pass both ctx & cfg
        }

        /* ---------- Auth ---------- */
        private void ConfigureAuthentication(ServiceConfigurationContext ctx)
        {
            ctx.Services.ForwardIdentityAuthenticationForBearer(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            ctx.Services.Configure<AbpClaimsPrincipalFactoryOptions>(o => o.IsDynamicClaimsEnabled = true);

            ctx.Services.Configure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme,
                o =>
                {
                    o.Cookie.SameSite = SameSiteMode.None;
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });
        }

        /* ---------- Bundles ---------- */
        private void ConfigureBundles()
        {
            Configure<AbpBundlingOptions>(o =>
            {
                o.StyleBundles.Configure(
                    BasicThemeBundles.Styles.Global,
                    b => b.AddFiles("/global-styles.css"));
            });
        }

        /* ---------- URLs ---------- */
        private void ConfigureUrls(IConfiguration cfg)
        {
            Configure<AppUrlOptions>(o =>
            {
                o.Applications["MVC"].RootUrl = cfg["App:SelfUrl"];
                o.RedirectAllowedUrls.AddRange(
                    cfg["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
                o.Applications["Angular"].RootUrl = cfg["App:ClientUrl"];
                o.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
            });
        }

        /* ---------- VFS in dev ---------- */
        private void ConfigureVirtualFileSystem(ServiceConfigurationContext ctx)
        {
            var env = ctx.Services.GetHostingEnvironment();
            if (!env.IsDevelopment()) return;

            Configure<AbpVirtualFileSystemOptions>(o =>
            {
                o.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainSharedModule>(
                    Path.Combine(env.ContentRootPath, "..", "web_backend.Domain.Shared"));
                o.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainModule>(
                    Path.Combine(env.ContentRootPath, "..", "web_backend.Domain"));
                o.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationContractsModule>(
                    Path.Combine(env.ContentRootPath, "..", "web_backend.Application.Contracts"));
                o.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationModule>(
                    Path.Combine(env.ContentRootPath, "..", "web_backend.Application"));
            });
        }

        /* ---------- HTTP clients ---------- */
        private void ConfigureHttpClients(ServiceConfigurationContext ctx)
        {
            ctx.Services.AddHttpClient("StreamProxy", c => c.Timeout = TimeSpan.FromSeconds(10));
        }

        /* ---------- Conventional controllers ---------- */
        private void ConfigureConventionalControllers()
        {
            Configure<AbpAspNetCoreMvcOptions>(o =>
            {
                o.ConventionalControllers.Create(typeof(web_backendApplicationModule).Assembly);
            });
        }

        /* ---------- Swagger ---------- */
        private static void ConfigureSwaggerServices(ServiceConfigurationContext ctx, IConfiguration cfg)
        {
            ctx.Services.AddAbpSwaggerGenWithOAuth(
                cfg["AuthServer:Authority"]!,
                new Dictionary<string, string> { { "web_backend", "web_backend API" } },
                o =>
                {
                    o.SwaggerDoc("v1", new OpenApiInfo { Title = "web_backend API", Version = "v1" });
                    o.DocInclusionPredicate((_, __) => true);
                    o.CustomSchemaIds(t => t.FullName);
                });
        }

        /* ---------- CORS ---------- */
        private void ConfigureCors(ServiceConfigurationContext ctx, IConfiguration cfg)
        {
            ctx.Services.AddCors(o =>
            {
                o.AddDefaultPolicy(b =>
                {
                    var origins = cfg["App:CorsOrigins"]?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.RemovePostFix("/"))
                        .Append("https://salmon‑glacier‑08dca301e.6.azurestaticapps.net")
                        .ToArray() ?? Array.Empty<string>();

                    b.WithOrigins(origins)
                     .WithAbpExposedHeaders()
                     .SetIsOriginAllowedToAllowWildcardSubdomains()
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
                });
            });
        }

        /* ---------- Azure Blob ---------- */

        private void ConfigureAzureBlobStorage(
            ServiceConfigurationContext ctx,
            IConfiguration cfg)
        {
            var conn = cfg["Storage:ConnectionString"];

            // SDK client (optional – useful for your own code)
            ctx.Services.AddSingleton(_ => new BlobServiceClient(conn));

            // ABP blob‑storing
            ctx.Services.Configure<AbpBlobStoringOptions>(opt =>
            {
                opt.Containers.ConfigureDefault(c =>
                {
                    c.UseAzure(az =>
                    {
                        az.ConnectionString = conn;
                        az.ContainerName = "videos";
                        az.CreateContainerIfNotExists = true;

                    });
                });
            });
        }
        /* ----------------------------------------------------------
           APP PIPELINE
        -----------------------------------------------------------*/
        public override void OnApplicationInitialization(ApplicationInitializationContext ctx)
        {
            var app = ctx.GetApplicationBuilder();
            var env = ctx.GetEnvironment();

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            else
            {
                app.UseExceptionHandler(ea =>
                {
                    ea.Run(async c =>
                    {
                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/html";
                        var ex = c.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                        if (ex != null)
                        {
                            var log = c.RequestServices.GetRequiredService<ILogger<web_backendHttpApiHostModule>>();
                            log.LogError(ex, "Unhandled exception");
                            await c.Response.WriteAsync($"<h1>{ex.Message}</h1><pre>{ex.StackTrace}</pre>");
                        }
                    });
                });
            }

            app.UseAbpRequestLocalization();
            app.UseCorrelationId();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();
            app.UseAbpOpenIddictValidation();

            if (MultiTenancyConsts.IsEnabled) app.UseMultiTenancy();

            app.UseUnitOfWork();
            app.UseDynamicClaims();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseAbpSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "web_backend API");
                var cfg = ctx.ServiceProvider.GetRequiredService<IConfiguration>();
                c.OAuthClientId(cfg["AuthServer:SwaggerClientId"]);
                c.OAuthScopes("web_backend");
            });

            app.UseAuditing();
            app.UseAbpSerilogEnrichers();
            app.UseConfiguredEndpoints();
        }
    }
}
