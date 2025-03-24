using MonkMode.Domain.Models;
using System.Threading.Tasks;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, NotificationType type, string message, string relatedEntityId = null);
        Task SendRealTimeNotificationAsync(Notification notification);
        Task MarkAllNotificationsAsReadAsync(int userId);
        Task ClearNotificationsAsync(int userId, bool readOnly = true);
        Task CreateTaskReminderNotificationAsync(int userId, TaskItem task);
        Task CreateFriendRequestNotificationAsync(int userId, Friendship friendship);
        Task CreateFriendAcceptedNotificationAsync(int userId, Friendship friendship);
    }
}