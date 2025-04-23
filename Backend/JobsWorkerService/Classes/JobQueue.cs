using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;

namespace JobsWorkerService.Classes
{
    /// <summary>
    /// JobQueue manages jobs using a priority queue, where higher priority jobs are processed first.
    /// If priorities are equal, jobs are processed in first-come-first-serve (FCFS) order.
    /// The priority is represented as a tuple of JobPriority (first) & QueuingTime (second).
    /// </summary>

    public class JobQueue
    {
        private readonly PriorityQueue<Job, (JobPriority, long)> _queue = new();

        public void Enqueue(JobPriority priority, Guid jobId)
        {
            long queueingTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var job = new Job(priority, queueingTime, jobId);

            _queue.Enqueue(job, (priority, job.QueuingTime));
        }

        public Job Dequeue()
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return _queue.Dequeue();
        }

        public int Count => _queue.Count;
    }
}