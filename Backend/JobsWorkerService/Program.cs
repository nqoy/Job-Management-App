using JobsClassLibrary.Classes;
using JobsClassLibrary.Utils;
using JobsWorkerService.Classes;
using JobsWorkerService.Clients;
using JobsWorkerService.Factories;
using JobsWorkerService.Handlers;
using JobsWorkerService.Managers;
using Microsoft.Extensions.Logging.Console;

var builder = Host.CreateApplicationBuilder(args);

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

// Register DI components
builder.Services.Configure<SignalRSettings>(
    builder.Configuration.GetSection("SignalR"));
builder.Services.AddSingleton<SignalRClient>();
builder.Services.AddSingleton<SignalRNotifier>();
builder.Services.AddSingleton<WorkerNodeFactory>();
builder.Services.AddSingleton<JobQueueManager>();
builder.Services.AddSingleton<ClientEventHandler>();

var host = builder.Build();

// Start the SignalRClient connection asynchronously
var signalRClient = host.Services.GetRequiredService<SignalRClient>();
await signalRClient.StartAsync();

// Init client handler to start the queue process
var clientEventHandler = host.Services.GetRequiredService<ClientEventHandler>();

// Run the application
await host.RunAsync();
