using JobsClassLibrary.Classes;
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

        private readonly int _maxWorkers;
        private readonly int _minWorkers;
        private readonly int _scaleCooldownSeconds;
        private long _lastScaleTime = 0;
        private readonly ILogger<JobQueueManager> _logger;

        private readonly SemaphoreSlim _jobsInQueueSignal = new(0);

        public JobQueueManager(IConfiguration configuration, WorkerNodeFactory workerFactory, ILogger<JobQueueManager> logger)
        {
            _token = _cancellationTokenSource.Token;
            _workerFactory = workerFactory;
            _logger = logger;

            var strinBuilder = new StringBuilder("JobQueueManager configuration:");

            void ReadSetting(string settingName, ref int fieldValue, int defaultValue)
            {
                string? stringValue = configuration[$"WorkerSettings:{settingName}"];

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
            _logger.LogInformation(strinBuilder.ToString());
            
            startQueue();
        }

        private void startQueue()
        {
            startInitialWorkerNodes();
            Task.Run(assignAndScaleLoop);
        }

        public void AddJobToQueue(Job job)
        {
            _jobQueue.Enqueue(job);
            _logger.LogDebug("Enqueued job {JobId} with priority {Priority}. Queue size is now {Count}.",
                             job.JobID, job.Priority, _jobQueue.Count);

            // signal the assign loop immediately
            _jobsInQueueSignal.Release();

            scaleWorkers();
        }

        private void startInitialWorkerNodes()
        {
            _logger.LogInformation("Starting {Count} initial worker nodes.", _minWorkers);
            for (int i = 0; i < _minWorkers; i++)
            {
                WorkerNode node = _workerFactory.Create(_token);
                _workerPool.Add(node);
                Task.Run( node.ProcessJobs, _token);
            }
        }

        public void StopAllWorkerNodes()
        {
            _logger.LogInformation("Stopping all worker nodes.");
            _cancellationTokenSource.Cancel();
            foreach (var node in _workerPool) 
                node.Stop();
        }

        private async Task assignAndScaleLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Wait until there's at least one job enqueued
                await _jobsInQueueSignal.WaitAsync(_token);

                while (_jobQueue.Count > 0)
                {
                    assignJobsToAvailableWorkers();
                    scaleWorkers();
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
                if (_jobQueue.Count == 0) break;
                Job job = _jobQueue.Dequeue();

                worker.AssignJob(job);
                assignedJobs++;
                _logger.LogDebug("Assigned job {JobId} to worker {WorkerId}.", job, worker.NodeID);
            }

            if (_jobQueue.Count > 0 && assignedJobs < availableWorkers.Count)
            {
                _logger.LogInformation(
                    "Jobs remain ({QueueCount}) after assigning {Assigned}/{Workers}. Scaling may be needed.",
                    _jobQueue.Count, assignedJobs, availableWorkers.Count);
                scaleWorkers();
            }
        }

        private void scaleWorkers()
        {
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (nowTimestamp - _lastScaleTime < _scaleCooldownSeconds) 
                return;
            int pendingJobCount = _jobQueue.Count, workersCount = _workerPool.Count;

            if (pendingJobCount > 500 && workersCount < _maxWorkers)
                addNewWorker();
            else if (pendingJobCount < 50 && workersCount > _minWorkers)
                removeWorker();

            _lastScaleTime = nowTimestamp;
        }

        private void addNewWorker()
        {
            if (_workerPool.Count >= _maxWorkers) 
                return;
            WorkerNode workerNode = _workerFactory.Create(_token);

            _workerPool.Add(workerNode);
            Task.Run(workerNode.ProcessJobs, _token);
            _logger.LogInformation("Scaling up: New worker added. Total: {Count}.", _workerPool.Count);
        }

        private void removeWorker()
        {
            if (_workerPool.Count <= _minWorkers) return;
            WorkerNode workerNode = _workerPool.Last();

            _workerPool.Remove(workerNode);
            workerNode.Stop();
            _logger.LogInformation("Scaling down: Worker {WorkerId} removed. Total: {Count}.", workerNode.NodeID, _workerPool.Count);
        }
    }
}
