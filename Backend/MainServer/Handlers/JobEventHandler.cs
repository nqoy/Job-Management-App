using JobsClassLibrary.Enums;
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
                [nameof(JobEvent.UpdateJobProgerss)] = HandleUpdateJobProgress,
                [nameof(JobEvent.UpdateJobStatus)] = HandleUpdateJobStatus,
                [nameof(JobEvent.StopJob)] = HandleStopJob
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

        private async Task HandleUpdateJobStatus(string serviceName, object payload)
        {
            if (!TryParseStatusPayload(payload, out Guid jobID, out JobStatus status))
            {
                _logger.LogWarning("Service '{Service}' sent invalid payload for UpdateJobStatus.", serviceName);
                return;
            }

            _logger.LogInformation("Service '{Service}' updating status to {Status} for job {JobID}.",
                serviceName, status, jobID);

            try
            {
                await _jobManager.UpdateJobStatusAsync(jobID, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status for job {JobID} to {Status}.", jobID, status);
            }
        }

        private async Task HandleUpdateJobProgress(string serviceName, object payload)
        {
            if (!TryParseProgressPayload(payload, out Guid jobID, out int jobProgress))
            {
                _logger.LogWarning("Service '{Service}' sent invalid payload for UpdateJobProgress.", serviceName);
                return;
            }

            _logger.LogInformation("Service '{Service}' updating progress to {JobProgress}% for job {JobID}.",
                serviceName, jobProgress, jobID);

            try
            {
                await _jobManager.UpdateJobProgressAsync(jobID, jobProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update progress for job {JobID} with progress {JobProgress}.", jobID, jobProgress);
            }
        }

        private async Task HandleStopJob(string serviceName, object payload)
        {
            if (payload is not Guid jobId)
            {
                _logger.LogWarning("Service '{Service}' sent invalid payload for StopJob.", serviceName);

                return;
            }

            _logger.LogInformation("Service '{Service}' stopping job {JobId}.", serviceName, jobId);
            await _jobManager.StopJobAsync(jobId);
        }

        private bool TryParseProgressPayload(object payload, out Guid jobID, out int jobProgress)
        {
            return TryParseJobPayload(payload, "progress", elem => elem.GetInt32(), out jobID, out jobProgress);
        }

        private bool TryParseStatusPayload(object payload, out Guid jobID, out JobStatus status)
        {
            return TryParseJobPayload(payload, "status", elem => (JobStatus)elem.GetInt32(), out jobID, out status);
        }

        private bool TryParseJobPayload<T>(object payload, string secondFieldName, Func<JsonElement, T> parseMethod, out Guid jobID, out T secondValue) where T : struct
        {
            jobID = default;
            secondValue = default;

            try
            {
                if (payload is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array && jsonElement.GetArrayLength() > 0)
                {
                    JsonElement payloadJsonObj = jsonElement[0];

                    if (!payloadJsonObj.TryGetProperty("jobID", out JsonElement jobIdElement) || jobIdElement.ValueKind != JsonValueKind.String)
                        return false;

                    if (!payloadJsonObj.TryGetProperty(secondFieldName, out JsonElement secondElement))
                        return false;

                    jobID = jobIdElement.GetGuid();
                    secondValue = parseMethod(secondElement);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse payload: {Payload}", payload);
                return false;
            }
        }
    }
}
