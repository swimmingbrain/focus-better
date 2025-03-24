using MonkMode.Domain.Models;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface IFriendshipService
    {
        Task<ServiceResult<Friendship>> SendFriendRequestAsync(int requesterId, string requesteeUserName);
        Task<ServiceResult<Friendship>> RespondToFriendRequestAsync(int friendshipId, int userId, bool accept);
        Task<FriendshipStats> GetFriendshipStatsAsync(int userId);
    }

    public class FriendshipStats
    {
        public int TotalFriends { get; set; }
        public int PendingIncoming { get; set; }
        public int PendingOutgoing { get; set; }
    }
}