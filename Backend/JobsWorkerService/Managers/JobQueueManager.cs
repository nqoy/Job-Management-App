using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using JobsWorkerService.Classes;
using System.Text;

public class JobQueueManager
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SemaphoreSlim _queueAssignSignal = new(0);
    private readonly CancellationToken _cancellationToken;
    private readonly List<WorkerNode> _workerPool = [];
    private readonly SignalRNotifier _signalRNotifier;
    private readonly ILogger<JobQueueManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JobQueue _jobQueue = new();

    private bool _queueChangedSinceLastBackup = false;
    private long _lastScaleTime = 0;

    // Settings
    private readonly int _maxWorkers;
    private readonly int _minWorkers;
    private readonly int _scaleCooldownSeconds;
    private readonly int _jobsToWorkerThreshold;
    private readonly int _sendQueueIntervalSeconds;

    public JobQueueManager(IConfiguration configuration, ILoggerFactory loggerFactory, ILogger<JobQueueManager> logger, SignalRNotifier signalRNotifier, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _signalRNotifier = signalRNotifier;
        _cancellationToken = _cancellationTokenSource.Token;
        _applicationLifetime = applicationLifetime;
        _applicationLifetime.ApplicationStopping.Register(backupQueueOnShutdown);

        var stringBuilder = new StringBuilder("JobQueueManager configuration:");

        void ReadSetting(string settingName, ref int fieldValue, int defaultValue)
        {
            string? stringValue = configuration[$"WorkerOptions:{settingName}"];

            if (!int.TryParse(stringValue, out int settingValue))
            {
                stringBuilder.Append($" Failed to read {settingName}, using default value of ({defaultValue});");
                fieldValue = defaultValue;
            }
            else
            {
                stringBuilder.Append($" {settingName}={settingValue};");
                fieldValue = settingValue;
            }
        }

        ReadSetting("MaxWorkers", ref _maxWorkers, 100);
        ReadSetting("MinWorkers", ref _minWorkers, 10);
        ReadSetting("ScaleCooldownSeconds", ref _scaleCooldownSeconds, 20);
        ReadSetting("JobsToWorkerRatioThreshold", ref _jobsToWorkerThreshold, 5);
        ReadSetting("BackupQueueIntervalSeconds", ref _sendQueueIntervalSeconds, 30);
        _logger.LogInformation(stringBuilder.ToString());

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
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_sendQueueIntervalSeconds * 1000, _cancellationToken);
                await backupQueueAsync();
            }
        }, _cancellationToken);
    }

    private void backupQueueOnShutdown()
    {
        try
        {
            _logger.LogInformation("Application stopping - backing up queue...");
            backupQueueAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to back up queue on shutdown.");
        }
    }

    private async Task backupQueueAsync()
    {
        try
        {
            if (!_queueChangedSinceLastBackup)
            {
                _logger.LogDebug("Skipping backup — no changes since last backup.");
                return;
            }

            _logger.LogDebug("Backing up job queue...");
            await _signalRNotifier.SendBackupJobQueue(_jobQueue.Serialize());

            _queueChangedSinceLastBackup = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to back up job queue.");
        }
    }

    public void AddJobToQueue(QueuedJob job)
    {
        _jobQueue.Enqueue(job);
        _queueChangedSinceLastBackup = true;
        sendJobInQueueUpdate(job);
        _logger.LogDebug("Enqueued job {JobId} with priority {Priority}. Queue size is now {Count}.",
            job.JobID, job.Priority, _jobQueue.Count);

        _queueAssignSignal.Release();
    }

    private void startInitialWorkerNodes()
    {
        _logger.LogInformation("Starting {Count} initial worker nodes.", _minWorkers);
        for (int i = 0; i < _minWorkers; i++)
        {
            addNewWorker();
        }
    }

    private async Task assignAndScaleLoop()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            await _queueAssignSignal.WaitAsync(_cancellationToken);
            bool isScalingDown = scaleWorkers();

            if (!isScalingDown)
            {
                assignJobsToAvailableWorkers();
            }
            await Task.Delay(500, _cancellationToken);
        }
    }

    private void assignJobsToAvailableWorkers()
    {
        List<WorkerNode> availableWorkers = _workerPool.Where(worker => worker.IsAvailable).ToList();
        int assignedJobs = 0;

        foreach (WorkerNode worker in availableWorkers)
        {
            Job job = _jobQueue.Dequeue();

            _queueChangedSinceLastBackup = true;
            worker.AssignJob(job);
            assignedJobs++;
            _logger.LogDebug("Assigned job [{JobId}] to worker: \n[{WorkerId}].", job.JobID, worker.NodeID);
        }
    }

    private bool scaleWorkers()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (now - _lastScaleTime < _scaleCooldownSeconds)
            return false;

        int workerCount = _workerPool.Count;
        int pendingJobsCount = _jobQueue.Count;
        int currentJobsToWorkerRatio = pendingJobsCount / (workerCount == 0 ? 1 : workerCount);

        try
        {
            bool isScalingUpNeeded = currentJobsToWorkerRatio > _jobsToWorkerThreshold && workerCount < _maxWorkers;

            if (isScalingUpNeeded)
            {
                int idealScaledUpWorkerCount = Math.Min(_maxWorkers, (int)Math.Ceiling((double)pendingJobsCount / _jobsToWorkerThreshold));
                int workersToAddCount = idealScaledUpWorkerCount - workerCount;

                if (workersToAddCount > 0)
                {
                    _logger.LogInformation("[SCALE UP]: Adding {WorkersToAdd} workers. Current: {WorkerCount}, PendingJobs: {PendingJobs}.",
                                            workersToAddCount, workerCount, pendingJobsCount);

                    for (int i = 0; i < workersToAddCount; i++)
                    {
                        addNewWorker();
                    }

                    _lastScaleTime = now;
                    _queueAssignSignal.Release();
                    _lastScaleTime = now;
                    printWorkerSummary();
                    return false;
                }
            }

            bool isScalingDownNeeded = currentJobsToWorkerRatio < _jobsToWorkerThreshold && workerCount > _minWorkers;

            if (isScalingDownNeeded)
            {
                int extraWorkerCount = Math.Max(_minWorkers, (int)Math.Ceiling((double)pendingJobsCount / _jobsToWorkerThreshold));
                int workersToRemoveCount = workerCount - extraWorkerCount;

                if (workersToRemoveCount > 0)
                {
                    List<WorkerNode> availableWorkersForRemoval = _workerPool
                        .Where(worker => worker.IsAvailable)
                        .Take(workersToRemoveCount)
                        .ToList();

                    if (availableWorkersForRemoval.Count > 0)
                    {
                        _logger.LogInformation("[SCALE DOWN]: Removing {WorkersToRemove} workers out of {workersToRemoveCount} extra. Current: {WorkerCount},  PendingJobs: {PendingJobs}.",
                                                                        availableWorkersForRemoval.Count, workersToRemoveCount, workerCount, pendingJobsCount);

                        foreach (WorkerNode worker in availableWorkersForRemoval)
                        {
                            removeWorker(worker);
                        }
                        _lastScaleTime = now;
                        printWorkerSummary();
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SCALE ERROR]: Exception occurred while scaling workers. Current: {WorkerCount}, PendingJobs: {PendingJobs}.",
                             workerCount, pendingJobsCount);
            return false;
        }
    }

    private void printWorkerSummary()
    {
        int currentJobWorkerRatio = _workerPool.Count == 0 ? _jobQueue.Count : _jobQueue.Count / _workerPool.Count;

        _logger.LogInformation("[WORKERS SUMMARY]:\n\tCurrent workers: {WorkerCount}\n\tPending jobs: {PendingJobs}\n\tJob-to-worker threshold: {Threshold}\n\tJob-to-worker ratio: {Ratio}",
                                _workerPool.Count, _jobQueue.Count, _jobsToWorkerThreshold, currentJobWorkerRatio);
    }

    private void addNewWorker()
    {
        if (_workerPool.Count >= _maxWorkers)
            return;

        WorkerNode workerNode = createWorkerNode();
        _workerPool.Add(workerNode);
        Task.Run(workerNode.ProcessJobsAsync, _cancellationToken);
        _logger.LogInformation("Scaling up: New worker added. Total: {Count}.", _workerPool.Count);
    }

    private void removeWorker(WorkerNode worker)
    {
        if (_workerPool.Contains(worker))
        {
            worker.StopJob();
            _workerPool.Remove(worker);
            _logger.LogInformation("Scaling down: Worker {WorkerId} removed. Total: {Count}.", worker.NodeID, _workerPool.Count);
        }
        else
        {
            _logger.LogWarning("Worker {WorkerId} not found in the pool for removal.", worker.NodeID);
        }
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
                _queueChangedSinceLastBackup = true;
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

    private void ResetAndReleaseSignal()
    {
        _queueAssignSignal.Wait(0);
        _queueAssignSignal.Release();
    }

    private WorkerNode createWorkerNode()
    {
        return new WorkerNode(
            _signalRNotifier,
            _loggerFactory.CreateLogger<WorkerNode>(),
            ResetAndReleaseSignal,
            _cancellationToken
        );
    }
}
