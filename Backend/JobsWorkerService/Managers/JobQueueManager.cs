using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using JobsWorkerService.Classes;
using JobsWorkerService.Factories;
using System.Text;

namespace JobsWorkerService.Managers
{
    public class JobQueueManager
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly List<WorkerNode> _workerPool = [];
        private readonly WorkerNodeFactory _workerFactory;
        private readonly JobQueue _jobQueue = new();
        private readonly CancellationToken _token;
        private readonly SignalRNotifier _signalRNotifier;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private long _lastScaleTime = 0;
        private readonly int _maxWorkers;
        private readonly int _minWorkers;
        private readonly int _scaleCooldownSeconds;
        private readonly int _jobsToWorkerRatioThreshold;
        private readonly int _sendQueueIntervalSeconds;
        private readonly ILogger<JobQueueManager> _logger;
        private readonly SemaphoreSlim _jobsInQueueSignal = new(0);

        public JobQueueManager(IConfiguration configuration, WorkerNodeFactory workerFactory, ILogger<JobQueueManager> logger, SignalRNotifier signalRNotifier, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _workerFactory = workerFactory;
            _signalRNotifier = signalRNotifier;
            _token = _cancellationTokenSource.Token;
            _applicationLifetime = applicationLifetime;
            _applicationLifetime.ApplicationStopping.Register(async () =>
            {
                await backupQueueAsync();
            });

            var strinBuilder = new StringBuilder("JobQueueManager configuration:");

            void ReadSetting(string settingName, ref int fieldValue, int defaultValue)
            {
                string? stringValue = configuration[$"WorkerOptions:{settingName}"];

                if (!int.TryParse(stringValue, out int settingValue))
                {
                    strinBuilder.Append($" Failed to read {settingName}, using default value of ({defaultValue});");
                    fieldValue = defaultValue;
                }
                else
                {
                    strinBuilder.Append($" {settingName}={settingValue};");
                    fieldValue = settingValue;
                }
            }

            ReadSetting("MaxWorkers", ref _maxWorkers, 100);
            ReadSetting("MinWorkers", ref _minWorkers, 10);
            ReadSetting("ScaleCooldownSeconds", ref _scaleCooldownSeconds, 30);
            ReadSetting("JobsToWorkerRatioThreshold", ref _jobsToWorkerRatioThreshold, 5);
            ReadSetting("SendRecoveryQueueIntervalSeconds", ref _sendQueueIntervalSeconds, 10);
            _logger.LogInformation(strinBuilder.ToString());

            startQueue();
            startPeriodicSendQueue();
        }

        private void startQueue()
        {
            startInitialWorkerNodes();
            Task.Run(assignAndScaleLoop);
        }

        private void startPeriodicSendQueue()
        {
            Task.Run(async () =>
            {
                while (!_token.IsCancellationRequested)
                {
                    await Task.Delay(_sendQueueIntervalSeconds * 1000, _token);

                    await backupQueueAsync();
                }
            }, _token);
        }

        private async Task backupQueueAsync()
        {
            try
            {
                _logger.LogDebug("Backing up job queue...");
                await _signalRNotifier.SendRecoverJobQueue(_jobQueue.Serialize());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to back up job queue.");
            }
        }

        public void AddJobToQueue(QueuedJob job)
        {
            _jobQueue.Enqueue(job);
            sendJobInQueueUpdate(job);
            _logger.LogDebug("Enqueued job {JobId} with priority {Priority}. Queue size is now {Count}.",
                job.JobID, job.Priority, _jobQueue.Count);

            // Signal assignAndScaleLoop to stop waiting immediately
            _jobsInQueueSignal.Release();
        }

        private void startInitialWorkerNodes()
        {
            _logger.LogInformation("Starting {Count} initial worker nodes.", _minWorkers);
            for (int i = 0; i < _minWorkers; i++)
            {
                WorkerNode node = _workerFactory.Create(_token);
                _workerPool.Add(node);
                Task.Run(node.ProcessJobsAsync, _token);
            }
        }

        private async Task assignAndScaleLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Wait for at least one job in queue
                await _jobsInQueueSignal.WaitAsync(_token);

                while (_jobQueue.Count > 0)
                {
                    assignJobsToAvailableWorkers();
                    // Minimal delay to avoid excessive CPU usage
                    await Task.Delay(500, _token);
                }
            }
        }

        private void assignJobsToAvailableWorkers()
        {
            List<WorkerNode> availableWorkers = _workerPool.Where(worker => worker.IsAvailable).ToList();
            int assignedJobs = 0;

            foreach (WorkerNode worker in availableWorkers)
            {
                if (_jobQueue.Count == 0)
                    break;
                Job job = _jobQueue.Dequeue();

                worker.AssignJob(job);
                assignedJobs++;
                availableWorkers.Remove(worker);
                _logger.LogDebug("Assigned job {JobId} to worker {WorkerId}.", job.JobID, worker.NodeID);
            }

            if (_jobQueue.Count > 0 || availableWorkers.Count > 0)
            {
                _logger.LogDebug("Jobs remain ({QueueCount}) after assigning {Assigned}. Available workers: {AvailableWorkers}. Scaling may be needed.",
                    _jobQueue.Count, assignedJobs, availableWorkers.Count);

                scaleWorkers();
            }
        }

        private void scaleWorkers()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (now - _lastScaleTime < _scaleCooldownSeconds)
                return;

            int pendingJobs = _jobQueue.Count;
            int workerCount = _workerPool.Count;
            int currentjobsToWorkerRatio = pendingJobs / (workerCount == 0 ? 1 : workerCount);

            if (currentjobsToWorkerRatio > _jobsToWorkerRatioThreshold && workerCount < _maxWorkers) // Scale Up
            {
                _logger.LogDebug("Scaling up: jobs-to-worker ratio ({Ratio}) > threshold ({Threshold}), workers {Workers} < max {MaxWorkers}",
                    currentjobsToWorkerRatio, _jobsToWorkerRatioThreshold, workerCount, _maxWorkers);
                addNewWorker();
            }
            else if (currentjobsToWorkerRatio < _jobsToWorkerRatioThreshold && workerCount > _minWorkers) // Scale Down
            {
                _logger.LogDebug("Scaling down: jobs-to-worker ratio ({Ratio}) < threshold ({Threshold}), workers {Workers} > min {MinWorkers}",
                    currentjobsToWorkerRatio, _jobsToWorkerRatioThreshold, workerCount, _minWorkers);
                removeWorker();
            }

            _lastScaleTime = now;
        }

        private void addNewWorker()
        {
            if (_workerPool.Count >= _maxWorkers)
                return;
            WorkerNode workerNode = _workerFactory.Create(_token);

            _workerPool.Add(workerNode);
            Task.Run(workerNode.ProcessJobsAsync, _token);
            _logger.LogInformation("Scaling up: New worker added. Total: {Count}.", _workerPool.Count);
        }

        private void removeWorker()
        {
            if (_workerPool.Count <= _minWorkers)
                return;
            WorkerNode workerNode = _workerPool.Last();

            workerNode.StopJob();
            _workerPool.Remove(workerNode);
            _logger.LogInformation("Scaling down: Worker {WorkerId} removed. Total: {Count}.", workerNode.NodeID, _workerPool.Count);
        }

        internal void RecoverJobQueue(List<QueuedJob> jobs)
        {
            _jobQueue.RecoverQueue(jobs);
        }

        internal void StopJobAsync(Guid jobID)
        {
            WorkerNode? targetWorker = _workerPool.FirstOrDefault(worker => worker.CurrentJob?.JobID == jobID);

            if (targetWorker != null)
            {
                lock (targetWorker)
                {
                    targetWorker.StopJob();
                    _logger.LogInformation("Job {JobID} was running and has been requested to stop on worker {WorkerID}.", jobID, targetWorker.NodeID);
                }
            }
            else
            {
                bool isMarkedForRemoval;

                lock (_jobQueue)
                {
                    isMarkedForRemoval = _jobQueue.MarkJobForLazyRemove(jobID);
                }
                if (isMarkedForRemoval)
                {
                    _logger.LogInformation("Job {JobID} is marked for removal.", jobID);
                }
                else
                {
                    _logger.LogWarning("Job {JobID} was not found in the queue for removal.", jobID);
                }
            }
        }

        private void sendJobInQueueUpdate(QueuedJob job)
        {
            Task.Run(async () =>
            {
                try
                {
                    await _signalRNotifier.NotifyJobProgress(job.JobID, JobStatus.InQueue, 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send job progress notification for JobID {JobId}", job.JobID);
                }
            });
        }
    }
}
