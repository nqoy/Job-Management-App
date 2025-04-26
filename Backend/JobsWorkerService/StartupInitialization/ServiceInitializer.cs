using JobsClassLibrary.Classes;
using JobsClassLibrary.Utils;
using JobsWorkerService.Classes;
using JobsWorkerService.Clients;
using JobsWorkerService.Handlers;
using JobsWorkerService.Managers;
using Microsoft.Extensions.Logging.Console;

namespace JobsWorkerService.StartupInitialization;

public static class ServiceInitializer
{
    public static void ConfigureLogging(HostApplicationBuilder builder)
    {
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
    }

    public static void ConfigureServices(HostApplicationBuilder builder)
    {
        builder.Services.Configure<SignalRSettings>(
            builder.Configuration.GetSection("SignalR"));
        builder.Services.AddSingleton<SignalRClient>();
        builder.Services.AddSingleton<SignalRNotifier>();
        builder.Services.AddSingleton<JobQueueManager>();
        builder.Services.AddSingleton<ClientEventHandler>();
    }

    public static async Task ConfigureSignalREventsAsync(IHost host)
    {
        // Force registration of client event handlers before any events are sent
        _ = host.Services.GetRequiredService<ClientEventHandler>();

        var signalRClient = host.Services.GetRequiredService<SignalRClient>();

        signalRClient.OnFirstConnected += async () =>
        {
            var notifier = host.Services.GetRequiredService<SignalRNotifier>();
            await notifier.SendRecoverJobQueue();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Initial event sent after first SignalR connection.");
        };

        await signalRClient.StartAsync();
    }

}
