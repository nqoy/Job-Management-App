using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using JobsClassLibrary.Interfaces;
using JobsClassLibrary.Utils;
using MainServer.Managers;
using System.Text.Json;

namespace MainServer.Handlers
{
    public delegate Task EventHandlerDelegate(string serviceName, object payload);

    public class JobEventHandler
    {
        private readonly JobManager _jobManager;
        private readonly ILogger<JobEventHandler> _logger;
        private readonly Dictionary<string, EventHandlerDelegate> _handlers;

        public JobEventHandler(JobManager jobManager, ILogger<JobEventHandler> logger)
        {
            _logger = logger;
            _jobManager = jobManager;
            _handlers = new Dictionary<string, EventHandlerDelegate>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(JobEvent.UpdateJobProgress)] = HandleUpdateJobProgress,
                [nameof(JobEvent.RecoverJobQueue)] = HandleRecoverJobQeueu,
            };
        }

        public async Task HandleEventAsync(string eventType, object payload, string serviceName)
        {
            if (!_handlers.TryGetValue(eventType, out var handler))
            {
                _logger.LogError("Unknown event '{Event}' from service '{Service}'", eventType, serviceName);
                throw new InvalidOperationException($"Unknown event type: {eventType}");
            }

            try
            {
                await handler(serviceName, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event '{Event}' from service '{Service}'", eventType, serviceName);
                throw;
            }
        }

        private async Task HandleUpdateJobProgress(string serviceName, object payload)
        {
            if (!PayloadDeserializer.TryParsePayloadDynamic(payload, _logger, out JobProgress? jobProgress))
            {
                _logger.LogWarning("Service '{Service}' sent invalid payload for UpdateJobProgress.", serviceName);
                return;
            }

            try
            {
                if (jobProgress != null)
                {
                    await _jobManager.UpdateJobProgress(jobProgress.JobID, jobProgress.Status, jobProgress.Progress);
                }
                else
                {
                    _logger.LogWarning("JobProgress is null, unable to update progress.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update progress for job {JobID} with progress {JobProgress}.", jobProgress?.JobID, jobProgress?.Progress);
            }
        }

        private async Task HandleRecoverJobQeueu(string serviceName, object payload)
        {
            if (!PayloadDeserializer.TryParsePayloadDynamic(payload, _logger, out List<QueuedJob>? queuedJobs))
            {
                _logger.LogWarning("Service '{Service}' sent invalid payload for UpdateJobProgress.", serviceName);
                return;
            }

            try
            {
                if (queuedJobs != null)
                {
                    await _jobManager.SaveQueueBackupData(queuedJobs);
                }
                else
                {
                    _logger.LogWarning("JobProgress is null, unable to update progress.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send job queue backup : {ex}");
            }
        }
    }
}
