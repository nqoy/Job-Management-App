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
            {
                _logger.LogError("SignalR HubUrl is not configured.");
                throw new ArgumentNullException(nameof(settings), "SignalR HubUrl not configured.");
            }

            string connectionUrl = $"{fullHubUrl}?service=WorkerService";

            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(connectionUrl)
                    .WithAutomaticReconnect()
                    .Build();
                _logger.LogInformation("HubConnection built successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build HubConnection with URL {ConnectionUrl}", connectionUrl);
                throw;
            }
        }

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("Starting SignalR connection...");
                await _connection.StartAsync();
                _logger.LogInformation("SignalR Connection State={State}", _connection.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting SignalR connection.");
                throw;
            }
        }

        public async Task SendEvent(string eventName, params object[] args)
        {
            try
            {
                _logger.LogDebug("Invoking SignalR event {EventName} with args {Args}", eventName, args);
                await _connection.InvokeAsync(eventName, args);
                _logger.LogDebug("Successfully invoked SignalR event [{EventName}]", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SignalR event [{EventName}]", eventName);
                throw;
            }
        }

        public void RegisterEventHandler<T>(string eventName, Action<T> handlerFunc)
        {
            try
            {
                _connection.On(eventName, handlerFunc);
                _logger.LogDebug("Registered handler for [{EventName}] events", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering [{EventName}] handler", eventName);
                throw;
            }
        }

        public void RegisterEventHandler<T1, T2>(string eventName, Action<T1, T2> handlerFunc)
        {
            try
            {
                _connection.On(eventName, handlerFunc);
                _logger.LogDebug("Registered handler for [{EventName}] events", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering [{EventName}] handler", eventName);
                throw;
            }
        }
    }
}
