using JobsWorkerService.Handlers;
using JobsWorkerService.StartupInitialization;

var builder = Host.CreateApplicationBuilder(args);

ServiceInitializer.ConfigureLogging(builder);
ServiceInitializer.ConfigureServices(builder);

var host = builder.Build();

await ServiceInitializer.ConfigureSignalREventsAsync(host);

await host.RunAsync();
