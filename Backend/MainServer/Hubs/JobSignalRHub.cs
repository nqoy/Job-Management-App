using Microsoft.AspNetCore.SignalR;
using JobsClassLibrary.Enums;
using MainServer.Handlers;
using System.Text;

namespace MainServer.Hubs
{
    /// <summary>
    /// Serves as the connection point for clients.
    /// Subscribes connecting clients to groups based on the SystemService enum.
    /// Receives events from clients and forwards them to the JobEventHandler.
    /// </summary>
    public class JobSignalRHub(JobEventHandler jobEventListener, ILogger<JobSignalRHub> logger) : Hub
    {
        private readonly JobEventHandler _jobEventListener = jobEventListener;
        private readonly ILogger<JobSignalRHub> _logger = logger;
        private readonly List<string> _serviceNames = new(Enum.GetNames(typeof(SystemService)));

        public override async Task OnConnectedAsync()
        {
            string connectionId = Context.ConnectionId;
            string? serviceName = Context.GetHttpContext()?.Request.Query["service"].ToString();

            if (serviceName == null)
            {
                _logger.LogError("serviceName is null on hub connection");
                throw new ArgumentNullException(nameof(serviceName), "Service name is required for the connection.");
            }
            if (!_serviceNames.Contains(serviceName))
            {
                _logger.LogError($"serviceName '{serviceName}' is not in the list of system services.");
                throw new ArgumentException($"The provided service name '{serviceName}' is not valid for this connection.");
            }

            var logBuilder = new StringBuilder();

            await Groups.AddToGroupAsync(connectionId, serviceName);
            logBuilder.AppendFormat("Client [{0}] CONNECTED, id : {1}", serviceName , connectionId);

            await base.OnConnectedAsync();

            _logger.LogInformation(logBuilder.ToString());
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string connectionId = Context.ConnectionId;
            string? serviceName = Context.GetHttpContext()?.Request.Query["service"].ToString();

            if (serviceName == null)
            {
                _logger.LogError("serviceName is null on hub disconnection");
                return; 
            }
            var logBuilder = new StringBuilder();

            logBuilder.AppendFormat("Client [{0}] DISCONNECTED, id : {1} .", serviceName, connectionId);
            if (exception != null)
            {
                logBuilder.AppendFormat(" Disconnection reason: {0}", exception.Message);
            }
            _logger.LogInformation(logBuilder.ToString());
            await Groups.RemoveFromGroupAsync(connectionId, serviceName);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task HandleEvent(string eventType, object payload)
        {
            string? serviceName = Context.GetHttpContext()?.Request.Query["service"].ToString()
                                  ?? $"UnknownService with id : {Context.ConnectionId}";

            _logger.LogDebug(
                "Received event '{EventType}' from service '{ServiceName}' with payload: {@Payload}",
                eventType, serviceName, payload);

            try
            {
                await _jobEventListener.HandleEventAsync(eventType, payload, serviceName);

                _logger.LogDebug(
                    "Handled event '{EventType}' from service '{ServiceName}' successfully.",
                    eventType, serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling event '{EventType}' from service '{ServiceName}'.",
                    eventType, serviceName);
            }
        }

    }
}
