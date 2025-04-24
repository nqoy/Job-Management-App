using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using JobsWorkerService.Clients;
using JobsWorkerService.Managers;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation("Received stop request for {jobID} job", jobID);

            // Stop the jobs
        }

        private void handleRecoverJobQueue(string serializedQueue)
        {
            _logger.LogInformation("Received status update for {UpdateCount} jobs → {Status}", jobIds.Count, status);

            _jobQueueManager.RecoverJobQueue(serializedQueue);
        }
    }
}
