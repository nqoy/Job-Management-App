using MainServer.Managers;

namespace MainServer.StartupInitialization
{
    public class StartupBackgroundService(IServiceScopeFactory serviceScopeFactory, ILogger<StartupBackgroundService> logger) : BackgroundService
    {
        private readonly ILogger<StartupBackgroundService> _logger = logger;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                // Create a scope to resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                var jobManager = scope.ServiceProvider.GetRequiredService<JobManager>();  // Resolve JobManager

                _logger.LogInformation("Sending queued jobs to worker service on startup.");
                await jobManager.SendQueuedJobsToWorkerService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued jobs on startup.");
            }
        }
    }

}
