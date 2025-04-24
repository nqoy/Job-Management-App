using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;

namespace JobsWorkerService.Classes
{
    public class WorkerNode
    {
        public bool IsAvailable => currentJob == null;
        public string NodeID { get; } = $"WORKER_{Guid.NewGuid()}";
        private readonly SignalRNotifier _signalRNotifier;
        private readonly ILogger<WorkerNode> _logger;
        private readonly CancellationToken _cancellationToken;
        private Job? currentJob;

        public WorkerNode(SignalRNotifier signalRNotifier, ILogger<WorkerNode> logger, CancellationToken cancellationToken)
        {
            _signalRNotifier = signalRNotifier;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _logger.LogInformation("Worker {NodeID} created.", NodeID);
        }

        public void ProcessJobs()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (currentJob != null)
                {
                    _logger.LogInformation("Worker {NodeID} processing job {JobID}", NodeID, currentJob.JobID);
                    try
                    {
                        _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Running).Wait(_cancellationToken);
                        _logger.LogDebug("Worker {NodeID} reported RUNNING for job {JobID}", NodeID, currentJob.JobID);

                        Thread.Sleep(5000);  // Work Proccess Simulation

                        _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Completed).Wait(_cancellationToken);
                        _logger.LogInformation("Worker {NodeID} reported COMPLETED for job {JobID}", NodeID, currentJob.JobID);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Failed).Wait(_cancellationToken);
                            _logger.LogWarning("Worker {NodeID} reported FAILED for job {JobID}", NodeID, currentJob.JobID);
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogError(notifyEx, "Worker {NodeID} failed to report FAILED for job {JobID}", NodeID, currentJob.JobID);
                        }
                        _logger.LogError(ex, "Worker {NodeID} encountered an error while processing job {JobID}", NodeID, currentJob.JobID);
                    }
                    currentJob = null;
                }
                Thread.Sleep(100);
            }
            _logger.LogInformation("Worker {NodeID} received cancellation and is stopping.", NodeID);
        }

        public void AssignJob(Job job)
        {
            currentJob = job;
            _logger.LogDebug("Worker {NodeID} assigned job {JobID}", NodeID, job.JobID);
        }

        public void Stop()
        {
            _logger.LogInformation("Worker {NodeID} stop requested.", NodeID);
        }
    }
}
