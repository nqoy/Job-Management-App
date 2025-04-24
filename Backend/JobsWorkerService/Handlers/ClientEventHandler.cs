using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;
using JobsWorkerService.Managers;

namespace JobsWorkerService.Handlers
{
    internal class ClientEventHandler
    {
        private readonly SignalRClient _signalRClient;
        private readonly JobQueueManager _jobQueueManager;
        private readonly ILogger<ClientEventHandler> _logger;

        public ClientEventHandler(SignalRClient signalRClient, JobQueueManager jobQueueManager, ILogger<ClientEventHandler> logger)
        {
            _jobQueueManager = jobQueueManager;
            _signalRClient = signalRClient;

            _signalRClient.OnJobReceived(HandleJobsReceived);
            _signalRClient.OnStopJob(HandleStopJobs);
            _signalRClient.OnUpdateJobStatus(HandleJobStatusUpdate);
            _logger = logger;
        }

        private void HandleJobsReceived(List<Job> jobs)
        {
            _logger.LogInformation("Received {JobCount} jobs from SignalR", jobs.Count);

            foreach (Job job in jobs)
            {
                _jobQueueManager.AddJobToQueue(job);
            }
        }

        private void HandleStopJobs(List<Guid> jobIds)
        {
            _logger.LogInformation("Received stop request for {StopCount} jobs", jobIds.Count);

            // Stop the jobs
        }

        private void HandleJobStatusUpdate(List<Guid> jobIds, JobStatus status)
        {
            _logger.LogInformation("Received status update for {UpdateCount} jobs → {Status}", jobIds.Count, status);

            // Update job status
        }
    }
}
