using JobsClassLibrary.Classes;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace JobsWorkerService.Clients
{
    public class SignalRClient
    {
        private readonly HubConnection _connection;
        private readonly ILogger<SignalRClient> _logger;
        private bool _firstConnectionDone = false;
        public event Func<Task>? OnFirstConnected;
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
                _logger.LogInformation("HubConnection built.");
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

                if (!_firstConnectionDone)
                {
                    _firstConnectionDone = true;
                    if (OnFirstConnected is not null)
                    {
                        await OnFirstConnected.Invoke();
                    }
                }

                _connection.On<string, object>("HandleEvent", (eventName, payload) =>
                {
                    _logger.LogInformation("Event received: [{EventName}] with payload:\n{Payload}", eventName, payload);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while starting SignalR connection.");
                throw;
            }
        }

        public async Task SendEvent(string eventName, object payload)
        {
            try
            {
                await _connection.InvokeAsync("HandleEvent", eventName, payload);
                _logger.LogDebug("Sent [{EventName}] event with payload:\n{@Payload}", eventName, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SignalR event [{EventName}], error :\n{Message}", eventName, ex.Message);
                throw;
            }
        }

        public async Task SendEvent(string eventName)
        {
            try
            {
                await _connection.InvokeAsync("HandleEvent", eventName, null);
                _logger.LogDebug("Sent event [{EventName}]", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SignalR event [{EventName}], error :\n{Message}", eventName, ex.Message);
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
