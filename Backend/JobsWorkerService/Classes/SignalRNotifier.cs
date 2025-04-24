using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;

namespace JobsWorkerService.Classes
{
    public class SignalRNotifier(SignalRClient signalRClient, ILogger<SignalRNotifier> logger)
    {
        private readonly SignalRClient _signalRClient = signalRClient;
        private readonly ILogger<SignalRNotifier> _logger = logger;

        public async Task NotifyJobStatus(Guid jobID, JobStatus status)
        {
            object payload = new
            {
                JobID = jobID,
                Status = status
            };

            await sendEvent(JobEvent.UpdateJobStatus, payload);
        }

        private async Task sendEvent(JobEvent eventType, object payload)
        {
            try
            {
                _logger.LogInformation("Invoking event: {EventType} with payload: {Payload}", eventType, payload);

                await _signalRClient.SendEvent(eventType.ToString(), payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while invoking event: {EventType} with payload: {Payload}", eventType, payload);
            }
        }
    }
}
