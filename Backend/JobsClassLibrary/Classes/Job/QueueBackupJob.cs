
using JobsClassLibrary.Enums;

namespace JobsClassLibrary.Classes.Job
{
    public class QueueBackupJob
    {
        public Guid JobID { get; set; }
        public long QueuingTime { get; set; }
        public long BackupTimestamp { get; set; }
        public JobPriority Priority { get; set; }
    }
}
