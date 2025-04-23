using JobsClassLibrary.Enums;

namespace JobsClassLibrary.Classes
{
    public class Job : JobBase
    {
        public Guid JobID { get; set; }
        public JobStatus Status { get; set; }
        public long CreatedAt { get; set; }
        public long StartedAt { get; set; }
        public long CompletedAt { get; set; }
        public long QueuingTime { get; set; }
        public int Progress { get; set; }

        public Job(JobPriority priority, long queuingTime, Guid jobId)
        {
            JobID = jobId;
            Priority = priority;
            QueuingTime = queuingTime;
            Status = JobStatus.Pending; 
            Progress = 0;
        }

        public Job(){}

        public void UpdateProgress(int progress)
        {
            Progress = progress;
        }

        public void MarkCompleted()
        {
            Status = JobStatus.Completed;
            Progress = 100;
        }

        public void MarkFailed()
        {
            Status = JobStatus.Failed;
            Progress = 0;
        }
    }
}
