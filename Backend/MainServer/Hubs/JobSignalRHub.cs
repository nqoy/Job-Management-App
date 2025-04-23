using Microsoft.AspNetCore.SignalR;
using JobsClassLibrary.Enums;
using MainServer.Handlers;

namespace MainServer.Hubs
{
    // Serves as the connection point for clients
    // Subscribes connecting clients to groups based on SystemService enum
    // Receives events from clients and forwards them to the JobEventListener
    public class JobSignalRHub(JobEventHandler jobEventListener) : Hub
    {
        private readonly List<string> _groupsToSubscribe = new List<string>(Enum.GetNames(typeof(SystemService)));
        private readonly JobEventHandler _jobEventListener = jobEventListener;

        public override async Task OnConnectedAsync()
        {
            foreach (var group in _groupsToSubscribe)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, group);
            }

            await base.OnConnectedAsync();
        }

        public async Task HandleEvent(string eventType, object payload)
        {
            await _jobEventListener.HandleEventAsync(eventType, payload);
        }
    }
}
