using MonkMode.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Repositories
{
    public interface IFriendshipRepository : IRepository<Friendship>
    {
        Task<List<Friendship>> GetFriendshipsForUserAsync(int userId, FriendshipStatus? status = null);
        Task<List<Friendship>> GetIncomingRequestsAsync(int userId);
        Task<List<Friendship>> GetOutgoingRequestsAsync(int userId);
        Task<Friendship> GetFriendshipAsync(int userId1, int userId2);
        Task<List<User>> GetFriendsAsync(int userId);
    }
}