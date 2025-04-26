
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
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                // Get Service Provider and do work
                // scope.ServiceProvider.GetRequiredService<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued jobs on startup.");
            }
        }
    }
}
