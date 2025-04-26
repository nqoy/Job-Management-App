using JobsClassLibrary.Classes;
using JobsClassLibrary.Utils;
using MainServer.DB;
using MainServer.Handlers;
using MainServer.Hubs;
using MainServer.Managers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

namespace MainServer.Initialization
{
    public static class AppInitializer
    {
        public static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register Custom Log Formatter
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                builder.Services.Configure<ConsoleFormatterOptions>(CustomConsoleFormatter.FormatterName, options =>
                {
                    options.IncludeScopes = true;
                });
                logging.AddConsole(options =>
                {
                    options.FormatterName = CustomConsoleFormatter.FormatterName;
                });
                logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
            });

            // Bind and register SignalRSettings
            SignalRSettings signalRConfig = builder.Configuration.GetSection("SignalR").Get<SignalRSettings>()
                                          ?? throw new InvalidOperationException("Configuration section 'SignalR' is missing or invalid.");

            builder.Services.AddSingleton(signalRConfig);
            builder.Services.AddSignalR();

            // DI registrations
            builder.Services.AddScoped<JobManager>();
            builder.Services.AddScoped<JobEventManager>();
            builder.Services.AddScoped<JobEventHandler>();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .SetIsOriginAllowed(_ => true);
                });
            });
            // Add Background Worker On Need
            // builder.Services.AddHostedService<StartupBackgroundService>();
        }

        public static void ConfigureApp(WebApplication app)
        {
            var signalRSettings = app.Services.GetRequiredService<SignalRSettings>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // CORS before endpoints & Auth
            app.UseCors();

            app.UseAuthorization();

            // Map SignalR hubs and controllers
            app.MapHub<JobSignalRHub>(signalRSettings.HubPath);
            app.MapControllers();
        }

        public static void LogServerUrls(WebApplication app)
        {
            var serverAddressesFeature = app.Services.GetRequiredService<IHostApplicationLifetime>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                if (app.Urls.Count != 0)
                {
                    logger.LogInformation("============================== MainServer listening on ==============================");
                    foreach (var address in app.Urls)
                    {
                        logger.LogInformation("Server URL: {Address}", address);
                    }
                    logger.LogInformation("=====================================================================================");
                }
                else
                {
                    logger.LogWarning("No URLs are configured for the server.");
                }
            });
        }
    }
}
