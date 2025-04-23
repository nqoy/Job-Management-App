using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace JobsWorkerService.Clients
{
    public class SignalRClient
    {
        private readonly HubConnection _connection;
        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        public SignalRClient(IOptions<SignalRSettings> settings)
        {
            string fullHubUrl = settings.Value.FullHubUrl;

            Console.WriteLine($"[SignalRClient] FullHubUrl = {fullHubUrl}");
            if (string.IsNullOrEmpty(fullHubUrl))
            {
                throw new ArgumentNullException(nameof(settings), "SignalR HubUrl is not configured in the app settings.");
            }
            string service = "WorkerService";
            string connectionUrl = $"{fullHubUrl}?service={service}";

            _connection = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .WithAutomaticReconnect()
                .Build();
        }

        public async Task StartAsync()
        {
            Console.WriteLine("[SignalRClient] Starting connection…");
            await _connection.StartAsync();
            Console.WriteLine("[SignalRClient] Connection State = " + _connection.State);
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

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}