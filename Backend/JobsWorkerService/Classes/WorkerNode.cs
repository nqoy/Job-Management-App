using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;

namespace JobsWorkerService.Classes
{
    public class WorkerNode(SignalRNotifier signalRNotifier, CancellationToken cancellationToken)
    {
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private readonly SignalRNotifier _signalRNotifier = signalRNotifier;
        private Job? _currentJob;

        public bool IsAvailable => _currentJob == null;

        public void ProcessJobs(JobQueue jobQueue)
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_currentJob != null)
                {
                    Console.WriteLine($"Processing job {_currentJob.JobID}");

                    // Report job started
                    _signalRNotifier.NotifyJobStatus(_currentJob.JobID, JobStatus.Running).Wait();

                    Thread.Sleep(5000); // Simulate processing

                    // Report job completed
                    _signalRNotifier.NotifyJobStatus(_currentJob.JobID, JobStatus.Completed).Wait();

                    _currentJob = null;
                }

                Thread.Sleep(100);
            }
        }

        public void AssignJob(Job job)
        {
            _currentJob = job;
        }

        public void Stop()
        {
            Console.WriteLine("Worker stopping.");
        }
    }
}
