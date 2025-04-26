using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;


namespace JobsWorkerService.Classes
{
    public class WorkerNode
    {
        public bool IsAvailable => CurrentJob == null;
        public string NodeID { get; } = $"WORKER_{Guid.NewGuid()}";
        private readonly SignalRNotifier _signalRNotifier;
        private readonly ILogger<WorkerNode> _logger;

        // Cancellation tokens
        private readonly CancellationToken _serviceCancellationToken;
        private CancellationTokenSource? _jobCancellationTokenSource;
        private CancellationToken jobCancellationToken => _jobCancellationTokenSource?.Token ?? CancellationToken.None;

        public Job? CurrentJob;

        private readonly Random _random = new(); // MOCK process timer.

        public WorkerNode(SignalRNotifier signalRNotifier, ILogger<WorkerNode> logger, CancellationToken serviceCancellationToken)
        {
            _signalRNotifier = signalRNotifier;
            _logger = logger;
            _serviceCancellationToken = serviceCancellationToken;
            _logger.LogInformation("Worker {NodeID} created.", NodeID);
        }

        private async Task RunMockProcessing(Guid jobId)
        {
            int totalTimeMs = _random.Next(1000, 600000);
            int stepTime = totalTimeMs / 100;

            for (int i = 1; i < 100; i++) // Iterate up to 100 progress steps
            {
                if (CurrentJob == null || jobCancellationToken.IsCancellationRequested || _serviceCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                CurrentJob.Progress = i;
                await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Running, i);
                await Task.Delay(stepTime, jobCancellationToken);
            }
        }

        public async Task ProcessJobsAsync()
        {
            while (!_serviceCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, _serviceCancellationToken);

                if (CurrentJob != null)
                {
                    Guid jobId = CurrentJob.JobID;

                    try
                    {
                        await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Running, 0);
                        _logger.LogDebug("Worker {NodeID} started job {JobID}", NodeID, jobId);

                        await RunMockProcessing(jobId);

                        await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Completed, 100);
                    }
                    catch (OperationCanceledException) when (_serviceCancellationToken.IsCancellationRequested)
                    {
                        await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Failed, CurrentJob.Progress);
                        _logger.LogInformation("Worker {NodeID} stopped job {JobID} due to service cancellation.", NodeID, jobId);
                    }
                    catch (OperationCanceledException) when (jobCancellationToken.IsCancellationRequested)
                    {
                        await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Stopped, CurrentJob.Progress);
                        _logger.LogInformation("Worker {NodeID} stopped job {JobID} due to job cancellation.", NodeID, jobId);
                    }
                    catch (Exception ex)
                    {
                        await _signalRNotifier.NotifyJobProgress(jobId, JobStatus.Failed, CurrentJob.Progress);
                        _logger.LogError(ex, "Worker {NodeID} encountered error on job {JobID}", NodeID, jobId);
                    }
                    finally
                    {
                        CurrentJob = null;
                        _jobCancellationTokenSource?.Dispose();
                        _jobCancellationTokenSource = null;
                    }
                }
            }

            _logger.LogDebug("Worker {NodeID} exiting ProcessJobsAsync loop due to service cancellation.", NodeID);
        }


        public void AssignJob(Job job)
        {
            if (CurrentJob != null)
            {
                _logger.LogWarning("Worker {NodeID} is already processing job {JobID}, cannot assign job {NewJobID}.", NodeID, CurrentJob.JobID, job.JobID);
                return;
            }
            _jobCancellationTokenSource?.Dispose();
            _jobCancellationTokenSource = new CancellationTokenSource();
            CurrentJob = job;
            // TODO : return after recover check
            //_logger.LogDebug("Worker {NodeID} assigned job {JobID}", NodeID, job.JobID);
        }

        public void StopJob()
        {
            if (CurrentJob == null)
            {
                _logger.LogWarning("Worker {NodeID} stop requested, but no job is currently assigned.", NodeID);
                return;
            }

            _jobCancellationTokenSource?.Cancel();
            _logger.LogDebug("Worker {NodeID} cancel requested for job {JobID}", NodeID, CurrentJob?.JobID);
        }
    }
}
