using JobsClassLibrary.Enums;
using JobsWorkerService.Classes;
using JobsWorkerService.Factories;


namespace JobsWorkerService.Managers
{
    public class JobQueueManager
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CancellationToken _cancellationToken;
        private readonly List<WorkerNode> _workerPool = [];
        private readonly WorkerNodeFactory _workerFactory;
        private readonly JobQueue _jobQueue = new();

        private readonly int _maxWorkers;
        private readonly int _minWorkers;
        private readonly int _scaleCooldownSeconds;
        private long _lastScaleTime = 0; // To throttle scaling actions

        public JobQueueManager(IConfiguration configuration, WorkerNodeFactory workerFactory)
        {
            _cancellationToken = _cancellationTokenSource.Token;
            _workerFactory = workerFactory;

            _maxWorkers = int.TryParse(configuration["WorkerSettings:MaxWorkers"], out var max)
                ? max : 100;

            _minWorkers = int.TryParse(configuration["WorkerSettings:MinWorkers"], out var min)
                ? min : 10;

            _scaleCooldownSeconds = int.TryParse(configuration["WorkerSettings:ScaleCooldownSeconds"], out var cooldown)
                ? cooldown : 30;
        }

        public void AddJobToQueue(JobPriority priority, Guid jobId)
        {
            _jobQueue.Enqueue(priority, jobId);
            AssignJobsToAvailableWorkers();
            ScaleWorkers(); // Ensure scaling happens after job is added
        }

        public void StartWorkerNodes()
        {
            // Start workers based on the initial count derived from the queue size
            for (int i = 0; i < _minWorkers; i++) // Start with the minimum workers
            {
                var workerNode = _workerFactory.Create(_cancellationToken);
                _workerPool.Add(workerNode);
                Task.Run(() => workerNode.ProcessJobs(_jobQueue), _cancellationToken);
            }
        }

        public void StopWorkerNodes()
        {
            _cancellationTokenSource.Cancel();
            // Gracefully stop all worker nodes
            foreach (var workerNode in _workerPool)
            {
                workerNode.Stop();
            }
        }

        private void AssignJobsToAvailableWorkers()
        {
            // Assign jobs to workers in parallel as needed
            var availableWorkers = _workerPool.Where(w => w.IsAvailable).ToList();
            int jobsAssigned = 0;

            foreach (var workerNode in availableWorkers)
            {
                if (_jobQueue.Count > 0)
                {
                    workerNode.AssignJob(_jobQueue.Dequeue());
                    jobsAssigned++;
                }
            }

            // If jobs are left in the queue, check for scaling
            if (_jobQueue.Count > 0 && jobsAssigned < availableWorkers.Count)
            {
                ScaleWorkers(); // Scale if necessary
            }
        }

        private void ScaleWorkers()
        {
            // Only scale if sufficient time has passed since the last scaling operation
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _lastScaleTime < _scaleCooldownSeconds) return;

            int queueSize = _jobQueue.Count;
            int currentWorkers = _workerPool.Count;

            // Scale up when the queue size is large and workers are not at max capacity
            if (queueSize > 500 && currentWorkers < _maxWorkers)
            {
                AddNewWorker();
            }
            // Scale down when the queue size is small and workers are more than the minimum
            else if (queueSize < 50 && currentWorkers > _minWorkers)
            {
                RemoveWorker();
            }

            _lastScaleTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Update last scale time
        }

        private void AddNewWorker()
        {
            if (_workerPool.Count < _maxWorkers)
            {
                var workerNode = _workerFactory.Create(_cancellationToken);
                _workerPool.Add(workerNode);
                Task.Run(() => workerNode.ProcessJobs(_jobQueue), _cancellationToken);
                Console.WriteLine("Scaling up: Added a new worker node.");
            }
        }

        private void RemoveWorker()
        {
            if (_workerPool.Count > _minWorkers)
            {
                var workerToRemove = _workerPool.Last();
                _workerPool.Remove(workerToRemove);
                workerToRemove.Stop();
                Console.WriteLine("Scaling down: Removed a worker node.");
            }
        }
    }
}
