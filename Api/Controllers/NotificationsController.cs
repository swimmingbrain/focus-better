using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.DTOs;
using MonkMode.Infrastructure.RealTime;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly HubConnectionManager _connectionManager;

        public NotificationsController(
            INotificationRepository notificationRepository,
            INotificationService notificationService,
            IHubContext<NotificationHub> notificationHub,
            HubConnectionManager connectionManager)
        {
            _notificationRepository = notificationRepository;
            _notificationService = notificationService;
            _notificationHub = notificationHub;
            _connectionManager = connectionManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int limit = 50)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var notifications = await _notificationRepository.GetNotificationsForUserAsync(
                userId, unreadOnly, limit);

            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Message = n.Message,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            return notificationDtos;
        }

        [HttpGet("count")]
        public async Task<ActionResult<NotificationCountDto>> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var count = await _notificationRepository.GetUnreadCountForUserAsync(userId);

            return new NotificationCountDto
            {
                UnreadCount = count
            };
        }

        [HttpPost("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notification = await _notificationRepository.FindByIdAsync(id);

            if (notification == null)
                return NotFound();

            // make sure notification gehört zu user
            if (notification.UserId != userId)
                return Forbid();

            // already read?
            if (notification.IsRead)
                return NoContent();

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);

            // update unread count
            var unreadCount = await _notificationRepository.GetUnreadCountForUserAsync(userId);

            // notify connected clients
            var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connectionIds.Any())
            {
                await _notificationHub.Clients.Clients(connectionIds).SendAsync(
                    "UnreadCountUpdated",
                    new NotificationCountDto { UnreadCount = unreadCount });
            }

            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            await _notificationService.MarkAllNotificationsAsReadAsync(userId);

            // notify connected clients
            var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connectionIds.Any())
            {
                await _notificationHub.Clients.Clients(connectionIds).SendAsync(
                    "UnreadCountUpdated",
                    new NotificationCountDto { UnreadCount = 0 });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notification = await _notificationRepository.FindByIdAsync(id);

            if (notification == null)
                return NotFound();

            // make sure notification gehört zu user
            if (notification.UserId != userId)
                return Forbid();

            await _notificationRepository.DeleteAsync(notification);

            // update unread count
            if (!notification.IsRead)
            {
                var unreadCount = await _notificationRepository.GetUnreadCountForUserAsync(userId);

                // notify connected clients
                var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
                if (connectionIds.Any())
                {
                    await _notificationHub.Clients.Clients(connectionIds).SendAsync(
                        "UnreadCountUpdated",
                        new NotificationCountDto { UnreadCount = unreadCount });
                }
            }

            return NoContent();
        }

        [HttpDelete]
        public async Task<ActionResult> ClearNotifications([FromQuery] bool readOnly = true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            await _notificationService.ClearNotificationsAsync(userId, readOnly);

            // update unread count
            if (!readOnly)
            {
                // notify connected clients
                var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
                if (connectionIds.Any())
                {
                    await _notificationHub.Clients.Clients(connectionIds).SendAsync(
                        "UnreadCountUpdated",
                        new NotificationCountDto { UnreadCount = 0 });
                }
            }

            return NoContent();
        }
    }
}