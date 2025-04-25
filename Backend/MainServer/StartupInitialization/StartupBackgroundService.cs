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
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                JobManager jobManager = scope.ServiceProvider.GetRequiredService<JobManager>();

                _logger.LogInformation("Sending queued jobs to worker service on startup.");
                await jobManager.SendRecoveryQueuedJobsToWorkerService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued jobs on startup.");
            }
        }
    }

}
