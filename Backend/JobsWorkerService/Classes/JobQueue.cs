using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using Newtonsoft.Json;

namespace JobsWorkerService.Classes
{
    internal class JobQueue
    {
        private readonly PriorityQueue<QueuedJob, (int, long)> _queue = new();
        private readonly HashSet<Guid> _jobsMarkedForRemoval = [];
        private readonly object _queueLock = new();

        internal void Enqueue(QueuedJob job)
        {
            job.QueuingTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            lock (_queueLock)
            {
                // Priority queue handles the smallest value as highest priority (min-heap)
                _queue.Enqueue(job, (-(int)job.Priority, job.QueuingTime));
            }
        }

        internal Job Dequeue()
        {
            lock (_queueLock)
            {
                while (_queue.Count > 0)
                {
                    QueuedJob job = _queue.Dequeue();

                    if (_jobsMarkedForRemoval.Contains(job.JobID))
                    {
                        _jobsMarkedForRemoval.Remove(job.JobID);
                        continue;
                    }

                    return job;
                }

                throw new InvalidOperationException("Queue is empty.");
            }
        }

        internal int Count
        {
            get
            {
                lock (_queueLock)
                {
                    return _queue.Count;
                }
            }
        }

        internal string Serialize()
        {
            lock (_queueLock)
            {
                return JsonConvert.SerializeObject(_queue.UnorderedItems
                    .Where(item => !_jobsMarkedForRemoval.Contains(item.Element.JobID))
                    .Select(item => new
                    {
                        item.Element.JobID,
                        item.Element.Priority,
                        item.Element.QueuingTime
                    }), Formatting.Indented);
            }
        }

        internal bool MarkJobForLazyRemove(Guid jobID)
        {
            lock (_queueLock)
            {
                if (_jobsMarkedForRemoval.Contains(jobID))
                {
                    return false;
                }

                _jobsMarkedForRemoval.Add(jobID);

                return true;
            }
        }
    }
}
