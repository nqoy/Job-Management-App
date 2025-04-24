using JobsClassLibrary.Enums;
using MainServer.Managers;
using Newtonsoft.Json.Linq;
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

            _logger.LogInformation("Service '{Service}' updating status to {Status} for job {Job}.",
                serviceName, status, jobID);

            await _jobManager.UpdateJobStatusAsync(jobID, status);
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

        private bool TryParseStatusPayload(object payload, out Guid jobID, out JobStatus status)
        {
            jobID = default;
            status = default;

            try
            {
                if (payload is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                {
                    if (jsonElement.GetArrayLength() == 0)
                        return false;
                    JsonElement payloadJsonObj = jsonElement[0];

                    jobID = payloadJsonObj.GetProperty("jobID").GetGuid();
                    status = (JobStatus)payloadJsonObj.GetProperty("status").GetInt32();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse UpdateJobStatus payload: {Payload}", payload);
                return false;
            }
        }
    }
}
