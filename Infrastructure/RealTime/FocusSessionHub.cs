using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonkMode.Domain.Repositories;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.RealTime
{
    [Authorize]
    public class FocusSessionHub : Hub
    {
        private readonly HubConnectionManager _connectionManager;
        private readonly IFocusSessionRepository _focusSessionRepository;

        public FocusSessionHub(
            HubConnectionManager connectionManager,
            IFocusSessionRepository focusSessionRepository)
        {
            _connectionManager = connectionManager;
            _focusSessionRepository = focusSessionRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.AddConnection(userId, Context.ConnectionId);

                // check if active session exists for user
                var activeSession = await _focusSessionRepository.GetActiveSessionForUserAsync(int.Parse(userId));

                if (activeSession != null)
                {
                    // send active session info to the connecting client
                    await Clients.Caller.SendAsync("ActiveSessionInfo", new
                    {
                        Id = activeSession.Id,
                        Mode = activeSession.Mode.ToString(),
                        StartTime = activeSession.StartTime,
                        ElapsedMinutes = (int)Math.Round((DateTime.UtcNow - activeSession.StartTime).TotalMinutes)
                    });
                }
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
        // - SessionStarted(FocusSessionDto session)
        // - SessionEnded(FocusSessionDto session)
        // - ActiveSessionInfo(object sessionInfo)
    }
}