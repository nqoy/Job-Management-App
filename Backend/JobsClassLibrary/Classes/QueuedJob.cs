
namespace JobsClassLibrary.Classes
{
    public class QueuedJob : Job
    {
        public long QueuingTime { get; set; } = 0;
    }
}
