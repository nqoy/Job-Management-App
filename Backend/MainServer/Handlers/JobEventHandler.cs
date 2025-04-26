using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using JobsClassLibrary.Utils;
using MainServer.Managers;

namespace MainServer.Handlers
{
    public delegate Task EventHandlerDelegate(string serviceName, object payload);
    public delegate Task EventHandlerNoPayloadDelegate(string serviceName);

    public class JobEventHandler
    {
        private readonly JobManager _jobManager;
        private readonly ILogger<JobEventHandler> _logger;
        private readonly Dictionary<string, EventHandlerDelegate> _handlersWithPayload;
        private readonly Dictionary<string, EventHandlerNoPayloadDelegate> _handlersWithoutPayload;

        public JobEventHandler(JobManager jobManager, ILogger<JobEventHandler> logger)
        {
            _logger = logger;
            _jobManager = jobManager;

            _handlersWithPayload = new Dictionary<string, EventHandlerDelegate>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(JobEvent.UpdateJobProgress)] = HandleUpdateJobProgress,
                [nameof(JobEvent.JobQueueBackup)] = HandleJobQueueBackup,
            };

            _handlersWithoutPayload = new Dictionary<string, EventHandlerNoPayloadDelegate>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(JobEvent.JobQeueuRecovery)] = HandleJobQueueRecovery,
            };
        }

        public async Task HandleEventAsync(string eventType, object? payload, string serviceName)
        {
            if (_handlersWithPayload.ContainsKey(eventType))
            {
                if (payload == null)
                {
                    _logger.LogWarning("Expected payload for event [{EventType}] but received null.", eventType);
                }

                var handlerWithPayload = _handlersWithPayload[eventType];
                await handlerWithPayload(serviceName, payload!);
                return;
            }

            if (_handlersWithoutPayload.TryGetValue(eventType, out var handlerWithoutPayload))
            {
                await handlerWithoutPayload(serviceName);
                return;
            }

            _logger.LogError("Unknown event [{EventType}] from [{ServiceName}]", eventType, serviceName);
            throw new InvalidOperationException($"Unknown event type: {eventType}");
        }

        private async Task HandleUpdateJobProgress(string serviceName, object payload)
        {

            if (!PayloadDeserializer.TryParsePayloadDynamic(payload, _logger, out JobProgress? jobProgress))
            {
                _logger.LogWarning("[{ServiceName}] sent invalid payload for UpdateJobProgress.", serviceName);
                return;
            }

            if (jobProgress != null)
            {
                try
                {
                    await _jobManager.UpdateJobProgress(jobProgress.JobID, jobProgress.Status, jobProgress.Progress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update progress for job {JobID} with progress [{JobProgress}%].", jobProgress.JobID, jobProgress.Progress);
                }
            }
            else
            {
                _logger.LogWarning("Parsed JobProgress is null, cannot update progress.");
            }
        }

        private async Task HandleJobQueueBackup(string serviceName, object payload)
        {
            if (!PayloadDeserializer.TryParsePayloadDynamic(payload, _logger, out List<QueueBackupJob>? queuedJobs))
            {
                _logger.LogWarning("[{ServiceName}] sent invalid payload for JobQueueBackup.", serviceName);
                return;
            }

            if (queuedJobs != null)
            {
                try
                {
                    await _jobManager.SaveQueueBackupData(queuedJobs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save job queue backup from service {ServiceName}.", serviceName);
                }
            }
            else
            {
                _logger.LogWarning("Parsed QueueBackupJobs is null, cannot save queue backup.");
            }
        }

        private async Task HandleJobQueueRecovery(string serviceName)
        {
            if (!string.Equals(serviceName, SystemService.WorkerService.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Received JobQueueRecovery event from unexpected service: {ServiceName}", serviceName);
                return;
            }

            try
            {
                await _jobManager.SendRecoveryQueuedJobsToWorkerService();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send recovery queued jobs to WorkerService.");
            }
        }
    }
}
