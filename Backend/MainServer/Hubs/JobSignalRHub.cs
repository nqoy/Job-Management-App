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
                return;  // Return early if the serviceName is null
            }

            var logBuilder = new StringBuilder();

            // Log client disconnection details
            logBuilder.AppendFormat("Client [{0}] DISCONNECTED, id : {1} .", serviceName, connectionId);

            if (exception != null)
            {
                logBuilder.AppendFormat(" Disconnection reason: {0}", exception.Message);
            }

            // Log the information to the logger
            _logger.LogInformation(logBuilder.ToString());

            // Optionally, remove the client from the group if needed
            await Groups.RemoveFromGroupAsync(connectionId, serviceName);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task HandleEvent(string eventType, object payload)
        {
            _logger.LogDebug(
                "Received event '{EventType}' from client {ConnectionId} with payload: {@Payload}",
                eventType, Context.ConnectionId, payload);

            try
            {
                await _jobEventListener.HandleEventAsync(eventType, payload);
                _logger.LogDebug("Handled event '{EventType}' successfully.", eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling event '{EventType}' from client {ConnectionId}.",
                    eventType, Context.ConnectionId);
            }
        }
    }
}
