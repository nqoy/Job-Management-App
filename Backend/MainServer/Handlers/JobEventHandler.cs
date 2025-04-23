using Microsoft.AspNetCore.SignalR;
using JobsClassLibrary.Enums;
using MainServer.Managers;


namespace MainServer.Handlers
{
    public class JobEventHandler(JobManager jobManager, ILogger<JobEventHandler> logger) : Hub
    {
        private readonly JobManager _jobManager = jobManager;
        private readonly ILogger<JobEventHandler> _logger = logger;

        public async Task HandleEventAsync(string eventType, object payload)
        {
            _logger.LogInformation("Handling event '{EventType}' with payload: {@Payload}", eventType, payload);

            try
            {
                switch (eventType)
                {
                    case nameof(JobEvent.UpdateJobStatus):
                        await HandleUpdateJobStatus(payload);
                        break;

                    case nameof(JobEvent.StopJob):
                        await HandleStopJob(payload);
                        break;

                    default:
                        _logger.LogError("Unknown event type: {EventType}", eventType);
                        throw new InvalidOperationException($"Unknown event type: {eventType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event '{EventType}'", eventType);
                throw;
            }
        }

        private async Task HandleUpdateJobStatus(object payload)
        {
            var statusPayload = payload as dynamic;

            if (statusPayload == null)
            {
                _logger.LogWarning("Invalid payload for UpdateJobStatus event.");
                return;
            }

            List<Guid>? jobIds;
            JobStatus jobStatus;

            try
            {
                jobIds = statusPayload?.JobIds.ToObject<List<Guid>>();
                jobStatus = statusPayload?.JobStatus.ToObject<JobStatus>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing the payload for UpdateJobStatus event.");
                return;
            }

            if (jobIds != null)
            {
                _logger.LogInformation("Updating job status for {JobCount} jobs.", jobIds.Count);
                await _jobManager.UpdateJobStatusAsync(jobIds, jobStatus);
            }
            else
            {
                _logger.LogWarning("Missing jobIds or jobStatus in payload for UpdateJobStatus.");
            }
        }

        private async Task HandleStopJob(object payload)
        {
            if (payload is Guid jobIdToStop)
            {
                _logger.LogInformation("Stopping {jobIdToStop} jobs.", jobIdToStop);
                await _jobManager.StopJobAsync(jobIdToStop);
            }
            else
            {
                _logger.LogWarning("Invalid or empty payload for StopJob event.");
            }
        }
    }
}
