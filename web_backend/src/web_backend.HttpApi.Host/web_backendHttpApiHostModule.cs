using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
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
using Volo.Abp.Account.Localization;
using Localization.Resources.AbpUi;

namespace web_backend;

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

        // Configure production OpenIddict settings for all environments
        Console.WriteLine("Running production OpenIddict configuration...");

        PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
        {
            // Disable automatic development certificates individually.
            options.AddDevelopmentEncryptionAndSigningCertificate = false;
            Console.WriteLine("Disabled development signing/encryption certificates.");
        });

        PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
        {
            try
            {
                var configuration = context.Services.GetConfiguration();
                var pfxPassword = configuration["OpenIddict:Certificates:Default:Password"];

                Console.WriteLine("Retrieved PFX password from configuration.");
                var certPath = Path.Combine(AppContext.BaseDirectory, "openiddict.pfx");

                Console.WriteLine($"Looking for certificate at: {certPath}");
                if (!File.Exists(certPath))
                {
                    Console.WriteLine("Certificate file not found!");
                    throw new FileNotFoundException("openiddict.pfx not found in expected location.", certPath);
                }

                var certificate = new X509Certificate2(
                    certPath,
                    "Varsity2024",
                    X509KeyStorageFlags.MachineKeySet
                );

                Console.WriteLine("Certificate loaded successfully.");

                serverBuilder.AddEncryptionCertificate(certificate);
                serverBuilder.AddSigningCertificate(certificate);
                
                // Explicitly configure OpenIddict endpoints to ensure they're accessible
                serverBuilder
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetIntrospectionEndpointUris("/connect/introspect")
                    .SetLogoutEndpointUris("/connect/logout")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserinfoEndpointUris("/connect/userinfo")
                    .SetVerificationEndpointUris("/connect/verify");
                
                // Explicitly allow client credentials and authorization code flows
                serverBuilder
                    .AllowAuthorizationCodeFlow()
                    .AllowClientCredentialsFlow()
                    .AllowRefreshTokenFlow();
                
                Console.WriteLine("Added encryption and signing certificates and configured endpoints.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while loading certificate:");
                Console.WriteLine(ex.ToString());
                throw;
            }
        });

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("web_backend");
                options.UseLocalServer();
                options.UseAspNetCore();
                Console.WriteLine("OpenIddict validation configured.");
            });
        });
        ObjectExtensionManager.Instance
        .MapEfCoreProperty<IdentityUser, string>(
            "Name",
            (entityBuilder, propertyBuilder) =>
            {
                propertyBuilder.HasMaxLength(IdentityUserConsts.MaxUserNameLength);
            });

        ObjectExtensionManager.Instance
            .MapEfCoreProperty<IdentityUser, string>(
                "PhoneNumber",
                (entityBuilder, propertyBuilder) =>
                {
                    propertyBuilder.HasMaxLength(IdentityUserConsts.MaxPhoneNumberLength);
                });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        // Configure response compression with Brotli and Gzip
        context.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/octet-stream", "application/wasm", "application/json" });
        });
        
        context.Services.Configure<BrotliCompressionProviderOptions>(options => 
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        context.Services.Configure<GzipCompressionProviderOptions>(options => 
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureLocalization();
        ConfigureSwaggerServices(context, configuration);
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                BasicThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}web_backend.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<web_backendDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}web_backend.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}web_backend.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<web_backendApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}web_backend.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(web_backendApplicationModule).Assembly);
        });
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AccountResource>()
                .AddBaseTypes(typeof(AbpUiResource));
                
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"web_backend", "web_backend API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "web_backend API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().RemovePostFix("/"))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
            
            // Add a more permissive policy for specific endpoints that need it
            options.AddPolicy("AllowAllOrigins", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
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

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        // Enable response compression to reduce payload sizes
        app.UseResponseCompression();
        
        app.UseCorrelationId();
        
        // Configure static files with cache headers
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Cache static resources for 7 days (604800 seconds)
                // Except for Blazor WebAssembly framework files which need special handling
                var path = ctx.File.PhysicalPath?.ToLower();
                if (path != null && 
                    (path.EndsWith(".js") || path.EndsWith(".css") || 
                     path.EndsWith(".woff") || path.EndsWith(".woff2") || 
                     path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".svg")))
                {
                    // Don't cache framework files with version in query string
                    if (!ctx.Context.Request.Path.Value!.Contains("_framework"))
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
                    }
                }
            }
        });
        
        app.UseRouting();
        
        // Use CORS before authentication
        app.UseCors();

        // Add middleware to handle HLS URL upgrades when needed
        app.Use(async (context, next) =>
        {
            // If this is an API request that might be returning HLS URLs
            if (context.Request.Path.StartsWithSegments("/api") && 
                context.Request.Method == "GET" && 
                context.Response.ContentType?.Contains("application/json") == true)
            {
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await next();

                responseBody.Seek(0, SeekOrigin.Begin);
                string responseText = await new StreamReader(responseBody).ReadToEndAsync();

                // Fix both HTTP to HTTPS and port 8080 to port 8443
                if (responseText.Contains("http://20.3.254.14:8080/hls"))
                {
                    responseText = responseText.Replace("http://20.3.254.14:8080/hls", "https://20.3.254.14:8443/hls");
                }
                // Also fix any HTTPS URLs that still use port 8080
                else if (responseText.Contains("https://20.3.254.14:8080/hls"))
                {
                    responseText = responseText.Replace("https://20.3.254.14:8080/hls", "https://20.3.254.14:8443/hls");
                }

                context.Response.Body = originalBodyStream;
                await context.Response.WriteAsync(responseText);
            }
            else
            {
                await next();
            }
        });
        
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        // Add middleware to handle authentication-related redirects
        app.Use(async (context, next) => 
        {
            // Check for login redirects and ensure they're properly handled
            if (context.Request.Path.StartsWithSegments("/Account/Login"))
            {
                var returnUrl = context.Request.Query["returnUrl"].ToString();
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    // Log the redirect for debugging
                    Console.WriteLine($"Login redirect with returnUrl: {returnUrl}");
                    
                    // Ensure the returnUrl is encoded if needed
                    if (!returnUrl.StartsWith("http"))
                    {
                        returnUrl = Uri.EscapeDataString(returnUrl);
                    }
                }
                
                // Continue with the request
                await next();
                
                // If we got a 404, it could be because ABP is handling the route differently
                if (context.Response.StatusCode == 404)
                {
                    Console.WriteLine("Login page returned 404, redirecting to identity server connect/authorize endpoint");
                    // Redirect to the OpenID Connect authorization endpoint
                    var clientId = "web_backend_Blazor";
                    var redirectUri = Uri.EscapeDataString($"{context.Request.Scheme}://{context.Request.Host}/authentication/login-callback");
                    var authUrl = $"/connect/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=openid%20profile%20email%20web_backend";
                    
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        authUrl += $"&state={Uri.EscapeDataString(returnUrl)}";
                    }
                    
                    context.Response.Redirect(authUrl);
                    return;
                }
            }
            // Handle redirect after successful login (302 Found status)
            else if (context.Request.Method == "POST" && context.Request.Path.StartsWithSegments("/Account/Login"))
            {
                // Capture response to check for redirect
                await next();
                
                // If this is a redirect response (302)
                if (context.Response.StatusCode == 302)
                {
                    // Get the form data to extract the returnUrl
                    string returnUrl = null;
                    if (context.Request.HasFormContentType)
                    {
                        var form = await context.Request.ReadFormAsync();
                        returnUrl = form["ReturnUrl"].ToString();
                    }
                    
                    // If no returnUrl in form, try query string
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        returnUrl = context.Request.Query["returnUrl"].ToString();
                    }

                    Console.WriteLine($"Login successful, redirecting to: {returnUrl}");
                    
                    // Override the Location header if returnUrl is specified
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        context.Response.Headers["Location"] = returnUrl;
                    }
                }
                
                return;
            }
            
            await next();
        });

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
