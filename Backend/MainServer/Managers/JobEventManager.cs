using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using MainServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MainServer.Managers
{
    public class JobEventManager(IHubContext<JobSignalRHub> hubContext)
    {
        private readonly IHubContext<JobSignalRHub> _hubContext = hubContext;

        public async Task SendJobsToWorkerService(List<Job> jobs)
        {
            await SendEvent(SystemService.WorkerService, JobEvent.JobRecived, jobs);
        }

        public async Task SendJobStatusUpdateToJobsApp(List<Guid> jobIds, JobStatus jobStatus)
        {
            var statusPayload = new { JobIds = jobIds, JobStatus = jobStatus };
            await SendEvent(SystemService.JobsApp, JobEvent.UpdateJobStatus, statusPayload);
        }

        public async Task SendStopJobToWorkerService(List<Guid> jobIds)
        {
            await SendEvent(SystemService.WorkerService, JobEvent.StopJob, jobIds);
        }

        private async Task SendEvent(SystemService service, JobEvent jobEvent, object payload)
        {
            await _hubContext.Clients.Group(service.ToString()).SendAsync(jobEvent.ToString(), payload);
        }
    }
}
