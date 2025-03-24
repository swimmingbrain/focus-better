using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.RealTime
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly HubConnectionManager _connectionManager;

        public NotificationHub(HubConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.AddConnection(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.RemoveConnection(userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // client methods that can be called from server:
        // - NewNotification(NotificationDto notification)
        // - UnreadCountUpdated(NotificationCountDto count)
    }
}