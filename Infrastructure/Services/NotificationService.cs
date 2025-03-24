using Microsoft.AspNetCore.SignalR;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.Infrastructure.RealTime;
using MonkMode.DTOs;
using MonkMode.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly HubConnectionManager _connectionManager;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> notificationHub,
            HubConnectionManager connectionManager)
        {
            _notificationRepository = notificationRepository;
            _notificationHub = notificationHub;
            _connectionManager = connectionManager;
        }

        public async Task<Notification> CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdNotification = await _notificationRepository.AddAsync(notification);

            // send real-time notification
            await SendRealTimeNotificationAsync(createdNotification);

            return createdNotification;
        }

        public async Task SendRealTimeNotificationAsync(Notification notification)
        {
            // get connection IDs for the user
            var userConnections = _connectionManager.GetConnectionsForUser(notification.UserId.ToString());

            if (userConnections.Any())
            {
                // create DTO for client
                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    Type = notification.Type.ToString(),
                    Message = notification.Message,
                    RelatedEntityId = notification.RelatedEntityId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                var unreadCount = await _notificationRepository.GetUnreadCountForUserAsync(notification.UserId);

                // send notification to connected clients
                await _notificationHub.Clients.Clients(userConnections).SendAsync(
                    "NewNotification",
                    notificationDto);

                // update unread count
                await _notificationHub.Clients.Clients(userConnections).SendAsync(
                    "UnreadCountUpdated",
                    new NotificationCountDto { UnreadCount = unreadCount });
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);

            // get connection IDs for the user
            var userConnections = _connectionManager.GetConnectionsForUser(userId.ToString());

            if (userConnections.Any())
            {
                // send updated unread count (0)
                await _notificationHub.Clients.Clients(userConnections).SendAsync(
                    "UnreadCountUpdated",
                    new NotificationCountDto { UnreadCount = 0 });
            }
        }

        public async Task ClearNotificationsAsync(int userId, bool readOnly = true)
        {
            await _notificationRepository.DeleteAllAsync(userId, readOnly);

            if (!readOnly)
            {
                var userConnections = _connectionManager.GetConnectionsForUser(userId.ToString());

                if (userConnections.Any())
                {
                    // send updated unread count (0)
                    await _notificationHub.Clients.Clients(userConnections).SendAsync(
                        "UnreadCountUpdated",
                        new NotificationCountDto { UnreadCount = 0 });
                }
            }
        }

        public async Task CreateTaskReminderNotificationAsync(int userId, TaskItem task)
        {
            var dueText = task.DueDate.HasValue
                ? $" due {task.DueDate.Value.ToString("MMM dd, yyyy")}"
                : "";

            await CreateNotificationAsync(
                userId,
                NotificationType.TASK_REMINDER,
                $"Reminder: '{task.Title}'{dueText}",
                task.Id.ToString());
        }

        public async Task CreateFriendRequestNotificationAsync(int userId, Friendship friendship)
        {
            await CreateNotificationAsync(
                userId,
                NotificationType.FRIEND_REQUEST,
                $"You have received a friend request",
                friendship.Id.ToString());
        }

        public async Task CreateFriendAcceptedNotificationAsync(int userId, Friendship friendship)
        {
            await CreateNotificationAsync(
                userId,
                NotificationType.FRIEND_ACCEPTED,
                $"Your friend request has been accepted",
                friendship.Id.ToString());
        }
    }
}