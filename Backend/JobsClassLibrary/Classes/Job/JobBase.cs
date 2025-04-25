using JobsClassLibrary.Enums;

namespace JobsClassLibrary.Classes.Job
{
    public class JobBase
    {
        public string Name { get; set; } = string.Empty;
        public JobPriority Priority { get; set; }
    }
}
