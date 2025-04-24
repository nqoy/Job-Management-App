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

        public ClientEventHandler(SignalRClient signalRClient, JobQueueManager jobQueueManager)
        {
            _jobQueueManager = jobQueueManager;
            _signalRClient = signalRClient;

            _signalRClient.OnJobReceived(HandleJobsReceived);
            _signalRClient.OnStopJob(HandleStopJobs);
            _signalRClient.OnUpdateJobStatus(HandleJobStatusUpdate);
        }

        private void HandleJobsReceived(List<Job> jobs)
        {
            Console.WriteLine($"Received {jobs.Count} jobs");
            foreach (Job job in jobs)
            {
            _jobQueueManager.AddJobToQueue(job);
            }
        }

        private void HandleStopJobs(List<Guid> jobIds)
        {
            Console.WriteLine($"Stopping {jobIds.Count} jobs");
            // Stop the jobs
        }

        private void HandleJobStatusUpdate(List<Guid> jobIds, JobStatus status)
        {
            Console.WriteLine($"Updating {jobIds.Count} jobs to status: {status}");
            // Update job status
        }
    }
}
