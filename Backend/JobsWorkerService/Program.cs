using JobsClassLibrary.Classes;
using JobsWorkerService.Classes;
using JobsWorkerService.Clients;
using JobsWorkerService.Factories;
using JobsWorkerService.Managers;

var builder = Host.CreateApplicationBuilder(args);

// Register DI components
builder.Services.Configure<SignalRSettings>(
    builder.Configuration.GetSection("SignalR"));
builder.Services.AddSingleton<SignalRClient>();
builder.Services.AddSingleton<SignalRNotifier>();
builder.Services.AddSingleton<WorkerNodeFactory>();
builder.Services.AddSingleton<JobQueueManager>();

var host = builder.Build();

// Start the SignalRClient connection asynchronously
var signalRClient = host.Services.GetRequiredService<SignalRClient>();
await signalRClient.StartAsync();

// Run the application
await host.RunAsync();
