using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Enums;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Data
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly AppDbContext _context;

        public FriendshipRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Friendship> AddAsync(Friendship entity)
        {
            await _context.Friendships.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(Friendship entity)
        {
            _context.Friendships.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Friendship>> FindAllAsync()
        {
            return await _context.Friendships.ToListAsync();
        }

        public async Task<Friendship> FindByIdAsync(int id)
        {
            return await _context.Friendships.FindAsync(id);
        }

        public async Task<Friendship> GetFriendshipAsync(int userId1, int userId2)
        {
            return await _context.Friendships
                .Where(f => (f.RequesterId == userId1 && f.RequesteeId == userId2) ||
                           (f.RequesterId == userId2 && f.RequesteeId == userId1))
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetFriendsAsync(int userId)
        {
            // get all accepted friendships
            var friendships = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.RequesteeId == userId) &&
                           f.Status == FriendshipStatus.ACCEPTED)
                .ToListAsync();

            // get (the other user in each friendship)
            var friendIds = friendships
                .Select(f => f.RequesterId == userId ? f.RequesteeId : f.RequesterId)
                .ToList();

            return await _context.Users
                .Where(u => friendIds.Contains(u.Id))
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetFriendshipsForUserAsync(int userId, FriendshipStatus? status = null)
        {
            var query = _context.Friendships
                .Where(f => f.RequesterId == userId || f.RequesteeId == userId);

            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Friendship>> GetIncomingRequestsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => f.RequesteeId == userId && f.Status == FriendshipStatus.PENDING)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetOutgoingRequestsAsync(int userId)
        {
            return await _context.Friendships
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.PENDING)
                .ToListAsync();
        }

        public async Task<Friendship> UpdateAsync(Friendship entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}