using JobsClassLibrary.Enums;

namespace JobsClassLibrary.Interfaces
{
    public interface IJobProgress
    {
        Guid JobID { get; set; }
        JobStatus Status { get; set; }
        int Progress { get; set; }
    }
}
