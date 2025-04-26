using JobsClassLibrary.Enums;

namespace JobsClassLibrary.Classes.Job
{
    // Used for backup payload
    public class QueueBackupJob
    {
        public Guid JobID { get; set; }
        public JobPriority Priority { get; set; }
        public long QueuingTime { get; set; }
    }
}