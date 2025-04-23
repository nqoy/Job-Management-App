using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;

namespace JobsWorkerService.Handlers
{
    internal class ClientEventHandler
    {
        private readonly SignalRClient _signalRClient;

        public ClientEventHandler(SignalRClient signalRClient)
        {
            _signalRClient = signalRClient;

            _signalRClient.OnJobReceived(HandleJobsReceived);
            _signalRClient.OnStopJob(HandleStopJobs);
            _signalRClient.OnUpdateJobStatus(HandleJobStatusUpdate);
        }

        private void HandleJobsReceived(List<Job> jobs)
        {
            Console.WriteLine($"Received {jobs.Count} jobs");
            // Process the jobs
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

        public async Task ConnectAsync()
        {
            await _signalRClient.StartAsync();
        }
    }
}
