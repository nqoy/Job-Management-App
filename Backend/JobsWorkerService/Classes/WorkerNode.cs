using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using System.Threading;
using System;

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

        private readonly Random _random = new(); // MOCK proeccess timer.
        public WorkerNode(SignalRNotifier signalRNotifier, ILogger<WorkerNode> logger, CancellationToken cancellationToken)
        {
            _signalRNotifier = signalRNotifier;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _logger.LogInformation("Worker {NodeID} created.", NodeID);
        }

        public async Task ProcessJobsAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (currentJob != null)
                {
                    _logger.LogDebug("Worker {NodeID} processing job {JobID}", NodeID, currentJob.JobID);
                    try
                    {
                        await _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Running);
                        _logger.LogDebug("Worker {NodeID} reported RUNNING for job {JobID}", NodeID, currentJob.JobID);

                        // Mock processing time between 1 second and 10 minutes
                        int totalTimeMs = _random.Next(1000, 600000);
                        int stepTime = totalTimeMs / 100;

                        for (int i = 1; i < 100 && !_cancellationToken.IsCancellationRequested; i++)
                        {
                            currentJob.Progress = i;
                            await _signalRNotifier.NotifyJobProgress(currentJob.JobID, i);
                            _logger.LogDebug("Worker {NodeID} reported {Progress}% progress for job {JobID}", NodeID, i, currentJob.JobID);
                            await Task.Delay(stepTime, _cancellationToken);
                        }

                        // Notify job status as Completed
                        await _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Completed);
                        _logger.LogDebug("Worker {NodeID} reported COMPLETED for job {JobID}", NodeID, currentJob.JobID);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await _signalRNotifier.NotifyJobStatus(currentJob.JobID, JobStatus.Failed);
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

                await Task.Delay(100, _cancellationToken);
            }

            _logger.LogDebug("Worker {NodeID} received cancellation and is stopping.", NodeID);
        }


        public void AssignJob(Job job)
        {
            currentJob = job;
            _logger.LogDebug("Worker {NodeID} assigned job {JobID}", NodeID, job.JobID);
        }

        public void Stop()
        {
            _logger.LogDebug("Worker {NodeID} stop requested.", NodeID);
        }
    }
}
