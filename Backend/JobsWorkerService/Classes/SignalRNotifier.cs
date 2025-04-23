using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;

namespace JobsWorkerService.Classes
{
    public class SignalRNotifier(SignalRClient signalRClient)
    {
        private readonly SignalRClient _signalRClient = signalRClient;

        public async Task NotifyJobStatus(Guid jobID, JobStatus status)
        {
            object payload = new
            {
                JobID = jobID,
                Status = status
            };

            await invokeEvent(JobEvent.UpdateJobStatus, payload);
        }

        private async Task invokeEvent(JobEvent eventType, object payload)
        {
            await _signalRClient.InvokeAsync(eventType.ToString(), eventType, payload);
        }
    }
}
