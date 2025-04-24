using JobsWorkerService.Classes;

namespace JobsWorkerService.Factories
{
    // Used to decouple JobQueueManager from SignalRNotifier
    public class WorkerNodeFactory(SignalRNotifier signalRNotifier, ILogger<WorkerNode> workerLogger)
    {
        private readonly SignalRNotifier _signalRNotifier = signalRNotifier;
        private readonly ILogger<WorkerNode> _workerLogger = workerLogger;
        public WorkerNode Create(CancellationToken cancellationToken)
        {
            return new WorkerNode(_signalRNotifier, _workerLogger, cancellationToken);
        }
    }
}
