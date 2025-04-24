using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using Newtonsoft.Json;

namespace JobsWorkerService.Classes
{
    /// <summary>
    /// JobQueue manages jobs using a priority queue, where higher priority jobs are processed first.
    /// If priorities are equal, jobs are processed in first-come-first-serve (FCFS) order.
    /// The priority is represented as a tuple of JobPriority (first) & QueuingTime (second).
    /// </summary>
    public class JobQueue
    {
        private readonly PriorityQueue<QueuedJob, (JobPriority, long)> _queue = new();

        public void Enqueue(QueuedJob job)
        {
            job.QueuingTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _queue.Enqueue(job, (job.Priority, job.QueuingTime));
        }

        public Job Dequeue()
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return _queue.Dequeue();
        }

        public int Count => _queue.Count;

        public string Serialize()
        {
            return JsonConvert.SerializeObject(_queue.UnorderedItems.Select(item => new
            {
                item.Element.JobID,
                item.Element.Priority,
                item.Element.QueuingTime,
            }));
        }

        public void RecoverQueue(string serializedQueue)
        {
            List<QueuedJob> jobs = JsonConvert.DeserializeObject<List<QueuedJob>>(serializedQueue);

            if (jobs != null && jobs.Count > 0)
            {
                _queue.Clear();
                foreach (QueuedJob job in jobs)
                {
                    _queue.Enqueue(job, (job.Priority, job.QueuingTime));
                }
            }
        }

    }
}
