using JobsWorkerService.Classes;

namespace JobsWorkerService.Factories
{
    // Used to decouple JobQueueManager from SignalRNotifier
    public class WorkerNodeFactory(SignalRNotifier signalRNotifier)
    {
        private readonly SignalRNotifier _signalRNotifier = signalRNotifier;

        public WorkerNode Create(CancellationToken cancellationToken)
        {
            return new WorkerNode(_signalRNotifier, cancellationToken);
        }
    }
}
