using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;
using JobsWorkerService.Managers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            _logger = logger;

            registerEventHandlers();
        }

        private void registerEventHandlers()
        {
            var eventHandlers = new List<(string EventName, Delegate Handler)>
            {
                (JobEvent.RecoverJobQueue.ToString(), handleRecoverJobQueue),
                (JobEvent.JobRecived.ToString(), handleJobsReceived),
                (JobEvent.StopJob.ToString(), handleStopJob),
            };

            foreach (var (eventName, handler) in eventHandlers)
            {
                registerHandlerByInputType(eventName, handler);
            }
        }

        private void registerHandlerByInputType(string eventName, Delegate handler)
        {
            if (handler is Action<List<QueuedJob>> jobHandler)
            {
                _signalRClient.RegisterEventHandler(eventName, jobHandler);
            }
            else if (handler is Action<List<Guid>> guidHandler)
            {
                _signalRClient.RegisterEventHandler(eventName, guidHandler);
            }
            else if (handler is Action<List<Guid>, JobStatus> statusHandler)
            {
                _signalRClient.RegisterEventHandler(eventName, statusHandler);
            }
        }

        private void handleJobsReceived(List<QueuedJob> jobs)
        {
            _logger.LogInformation("Received {JobCount} jobs from SignalR", jobs.Count);

            foreach (QueuedJob job in jobs)
            {
                _jobQueueManager.AddJobToQueue(job);
            }
        }

        private void handleStopJob(Guid jobID)
        {
            _logger.LogInformation("Received stop job event for {jobID} job.", jobID);
            _jobQueueManager.StopJobAsync(jobID);
        }

        private void handleRecoverJobQueue(string serializedQueue)
        {
            List<QueuedJob> jobs = JsonConvert.DeserializeObject<List<QueuedJob>>(serializedQueue);

            if (jobs != null)
            {
                _logger.LogInformation("Received status update for {UpdateCount} jobs.", jobs.Count);

                _jobQueueManager.RecoverJobQueue(jobs);
            }
            else
            {
                _logger.LogWarning("Failed to deserialize the job queue. No jobs were recovered.");
            }
        }
    }
}
