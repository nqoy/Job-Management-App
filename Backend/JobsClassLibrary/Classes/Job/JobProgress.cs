using JobsClassLibrary.Enums;
using JobsClassLibrary.Interfaces;

namespace JobsClassLibrary.Classes.Job
{
    public class JobProgress : IJobProgress
    {
        public Guid JobID { get; set; }
        public JobStatus Status { get; set; }
        public int Progress { get; set; }
    }
}
