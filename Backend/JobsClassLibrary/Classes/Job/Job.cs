using JobsClassLibrary.Enums;
using JobsClassLibrary.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace JobsClassLibrary.Classes.Job
{
    public class Job : JobBase, IJobProgress
    {
        [Key]
        public Guid JobID { get; set; }
        public JobStatus Status { get; set; }
        public int Progress { get; set; }
        public long CreatedAt { get; set; }
        public long StartedAt { get; set; }
        public long CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public Job() { }

        public void UpdateProgress(int progress)
        {
            Progress = progress;
        }

        public void MarkStarted()
        {
            StartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Status = JobStatus.Running;
            Progress = 1;
        }

        public void MarkCompleted()
        {
            CompletedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Status = JobStatus.Completed;
            Progress = 100;
        }

        public void MarkRestarted()
        {
            Status = JobStatus.Pending;
            StartedAt = 0;
            Progress = 0;
        }

        public void MarkProgress(JobStatus status, int progress, string ex = "")
        {
            Status = status;
            Progress = progress;
            ErrorMessage = status == JobStatus.Stopped
                ? $"Stop Time : {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                : $"Job Process Failed at : {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\n{ex}";
        }
    }
}
