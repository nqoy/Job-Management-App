namespace JobsClassLibrary.Classes.Job
{
    public class QueuedJob : Job
    {
        public long QueuingTime { get; set; } = 0;
    }
}
