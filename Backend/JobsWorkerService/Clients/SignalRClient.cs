using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
namespace JobsWorkerService.Clients
{
    public class SignalRClient
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRClient> _logger;
        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        public SignalRClient(IOptions<SignalRSettings> settings, ILogger<SignalRClient> logger)
        {
            _logger = logger;
            string fullHubUrl = settings.Value.FullHubUrl;

            _logger.LogInformation("SignalRClient FullHubUrl={FullHubUrl}", fullHubUrl);
            if (string.IsNullOrEmpty(fullHubUrl))
                throw new ArgumentNullException(nameof(settings), "SignalR HubUrl not configured.");

            string connectionUrl = $"{fullHubUrl}?service=WorkerService";

            _connection = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .WithAutomaticReconnect()
                .Build();
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting SignalR connection...");
            await _connection.StartAsync();
            _logger.LogInformation("SignalR Connection State={State}", _connection.State);
        }

        public async Task InvokeAsync(string eventName, params object[] args)
        {
            await _connection.InvokeAsync(eventName, args);
        }

        public void OnJobReceived(Action<List<Job>> handler)
        {
            _connection.On(JobEvent.JobRecived.ToString(), handler);
        }

        public void OnStopJob(Action<List<Guid>> handler)
        {
            _connection.On(JobEvent.StopJob.ToString(), handler);
        }

        public void OnUpdateJobStatus(Action<List<Guid>, JobStatus> handler)
        {
            _connection.On(JobEvent.UpdateJobStatus.ToString(), handler);
        }
    }
}