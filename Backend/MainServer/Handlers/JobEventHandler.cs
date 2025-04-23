using Microsoft.AspNetCore.SignalR;
using JobsClassLibrary.Enums;
using MainServer.Managers;

namespace MainServer.Handlers
{
    public class JobEventHandler(JobManager jobManager) : Hub
    {
        private readonly JobManager _jobManager = jobManager;

        public async Task HandleEventAsync(string eventType, object payload)
        {
            switch (eventType)
            {
                case nameof(JobEvent.UpdateJobStatus):
                    var statusPayload = payload as dynamic;
                    var jobIds = statusPayload?.JobIds.ToObject<List<Guid>>();
                    var jobStatus = statusPayload?.JobStatus.ToObject<JobStatus>();
                    if (jobIds != null && jobStatus != null)
                    {
                        // Call Function
                    }
                    break;

                case nameof(JobEvent.StopJob):
                    if (payload is List<Guid> jobIdsToStop)
                    {
                        // Call Function
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown event type: {eventType}");
            }
        }
    }
}
