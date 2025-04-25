using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using MainServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MainServer.Managers
{
    public class JobEventManager(IHubContext<JobSignalRHub> hubContext, ILogger<JobEventManager> logger)
    {
        private readonly IHubContext<JobSignalRHub> _hubContext = hubContext;
        private readonly ILogger<JobEventManager> _logger = logger;

        public async Task SendJobsToWorkerService(List<Job> jobs)
        {
            await SendEvent(SystemService.WorkerService, JobEvent.JobRecived, jobs);
        }

        public async Task SendJobStatusUpdateToJobsApp(Guid jobID, JobStatus jobStatus)
        {
            object statusPayload = new { JobID = jobID, JobStatus = jobStatus };

            await SendEvent(SystemService.JobsApp, JobEvent.UpdateJobStatus, statusPayload);
        }

        public async Task SendStopJobToWorkerService(Guid jobID)
        {
            await SendEvent(SystemService.WorkerService, JobEvent.StopJob, jobID);
        }

        private async Task SendEvent(SystemService service, JobEvent jobEvent, object payload)
        {
            string serviceToSend = service.ToString();
            string eventName = jobEvent.ToString();

            _logger.LogDebug("Sending '{Event}' to '{Service}'", eventName, serviceToSend);

            try
            {
                await _hubContext.Clients.Group(serviceToSend).SendAsync(eventName, payload);

                _logger.LogDebug("Sent '{Event}' to '{Service}' successfully", eventName, serviceToSend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending '{Event}' to '{Service}'", eventName, serviceToSend);
            }
        }
    }
}
