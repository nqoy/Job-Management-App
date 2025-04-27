using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;

namespace JobsWorkerService.Classes
{
    public class SignalRNotifier(SignalRClient signalRClient, ILogger<SignalRNotifier> logger)
    {
        private readonly SignalRClient _signalRClient = signalRClient;
        private readonly ILogger<SignalRNotifier> _logger = logger;

        internal async Task NotifyJobProgress(Guid jobID, JobStatus status, int jobProgress)
        {
            object payload = new
            {
                JobID = jobID,
                Status = status,
                Progress = jobProgress
            };

            await sendEvent(JobEvent.UpdateJobProgress, payload);
        }

        internal async Task SendBackupJobQueue(string serializedQueue)
        {
           
            await sendEvent(JobEvent.JobQueueBackup, serializedQueue);
        }

        internal async Task SendRecoverJobQueue()
        {

            await sendEvent(JobEvent.JobQueueRecovery);
        }

        private async Task sendEvent(JobEvent eventType)
        {
            try
            {
                _logger.LogInformation("Send event: [{EventType}]", eventType);

                await _signalRClient.SendEvent(eventType.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking event: {EventType}", eventType);
            }
        }

        private async Task sendEvent(JobEvent eventType, object payload)
        {
            try
            {
                await _signalRClient.SendEvent(eventType.ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking event: {EventType} with payload:\n{Payload}", eventType, payload);
            }
        }
    }
}
