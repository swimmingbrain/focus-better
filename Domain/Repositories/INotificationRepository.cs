using MonkMode.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<List<Notification>> GetNotificationsForUserAsync(int userId, bool unreadOnly = false, int limit = 50);
        Task<int> GetUnreadCountForUserAsync(int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAllAsync(int userId, bool readOnly = true);
    }
}