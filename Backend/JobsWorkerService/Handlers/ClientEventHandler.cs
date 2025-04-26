using JobsClassLibrary.Classes.Job;
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
            _logger = logger;

            registerEventHandlers();
        }

        private void registerEventHandlers()
        {
            var eventHandlers = new List<(string EventName, Delegate Handler)>
            {
                (JobEvent.JobRecive.ToString(), handleJobsReceived),
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
            else if (handler is Action<Guid> guidHandler)
            {
                _signalRClient.RegisterEventHandler(eventName, guidHandler);
            }
            else if (handler is Action<string> serilizedHandler)
            {
                _signalRClient.RegisterEventHandler(eventName, serilizedHandler);
            }
        }

        private void handleJobsReceived(List<QueuedJob> jobs)
        {
            _logger.LogInformation("Received [{event}] : {JobCount} jobs ", JobEvent.JobRecive, jobs.Count);

            foreach (QueuedJob job in jobs)
            {
                _jobQueueManager.AddJobToQueue(job);
            }
        }

        private void handleStopJob(Guid jobID)
        {
            _logger.LogInformation("Received [{event}] event for {jobID} job.",JobEvent.StopJob, jobID);
            _jobQueueManager.StopJobAsync(jobID);
        }
    }
}
