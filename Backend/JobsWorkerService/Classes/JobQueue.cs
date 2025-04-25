using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using Newtonsoft.Json;

namespace JobsWorkerService.Classes
{
    internal class JobQueue
    {
        private readonly PriorityQueue<QueuedJob, (JobPriority, long)> _queue = new();
        private readonly HashSet<Guid> _jobsMarkedForRemoval = [];
        private readonly object _queueLock = new();

        internal void Enqueue(QueuedJob job)
        {
            job.QueuingTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            lock (_queueLock)
            {
                _queue.Enqueue(job, (job.Priority, job.QueuingTime));
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

        internal void RecoverQueue(List<QueuedJob> jobs)
        {
            lock (_queueLock)
            {
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
