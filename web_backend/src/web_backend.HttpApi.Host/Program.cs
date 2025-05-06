using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using web_backend.Scoreboard;   // Hub namespace

namespace web_backend;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting web_backend.HttpApi.Host.");

            var builder = WebApplication.CreateBuilder(args);

            /* ↓↓↓  allow large multipart bodies (1 GB) */
            builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_073_741_824);

            builder.Host
                   .AddAppSettingsSecretsJson()
                   .UseAutofac()
                   .UseSerilog();

            // SignalR + open CORS for OBS overlay
            builder.Services.AddSignalR();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("OverlayCors", p =>
                {
                    p.AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials()
                     .SetIsOriginAllowed(_ => true);
                });
            });

            await builder.AddApplicationAsync<web_backendHttpApiHostModule>();

            var app = builder.Build();

            app.UseCors("OverlayCors");
            app.MapHub<ScoreboardHub>("/signalr/scoreboard");

            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
