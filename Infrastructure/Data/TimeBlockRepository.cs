using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Data
{
    public class TimeBlockRepository : ITimeBlockRepository
    {
        private readonly AppDbContext _context;

        public TimeBlockRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TimeBlock> AddAsync(TimeBlock entity)
        {
            // check if user exists by UserId
            var user = await _context.Users.FindAsync(entity.UserId);

            // if not, create new
            if (user == null)
            {
                // create new user with same id
                user = new User
                {
                    Id = entity.UserId,
                    UserName = $"User_{entity.UserId}",
                    Email = $"user{entity.UserId}@example.com"
                };

                // add new user to context and save
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // add TimeBlock
            await _context.TimeBlocks.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(TimeBlock entity)
        {
            _context.TimeBlocks.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TimeBlock>> FindAllAsync()
        {
            return await _context.TimeBlocks.ToListAsync();
        }

        public async Task<TimeBlock> FindByIdAsync(int id)
        {
            return await _context.TimeBlocks.FindAsync(id);
        }

        public async Task<List<TimeBlock>> GetTimeBlocksForUserAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.TimeBlocks
                .Where(tb => tb.UserId == userId &&
                           ((tb.StartTime >= startDate && tb.StartTime <= endDate) ||
                            (tb.EndTime >= startDate && tb.EndTime <= endDate) ||
                            (tb.StartTime <= startDate && tb.EndTime >= endDate)))
                .OrderBy(tb => tb.StartTime)
                .ToListAsync();
        }

        public async Task<TimeBlock> GetTimeBlockWithTasksAsync(int timeBlockId)
        {
            return await _context.TimeBlocks
                .Include(tb => tb.LinkedTasks)
                .FirstOrDefaultAsync(tb => tb.Id == timeBlockId);
        }

        public async Task<bool> HasOverlappingTimeBlocksAsync(int userId, DateTime startTime, DateTime endTime, int? excludeId = null)
        {
            var query = _context.TimeBlocks
                .Where(tb => tb.UserId == userId &&
                           !((tb.EndTime <= startTime) || (tb.StartTime >= endTime)));

            if (excludeId.HasValue)
            {
                query = query.Where(tb => tb.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<TimeBlock> UpdateAsync(TimeBlock entity)
        {
            // check if user exists by UserId
            var user = await _context.Users.FindAsync(entity.UserId);

            // if not, create new
            if (user == null)
            {
                // create new user with same id
                user = new User
                {
                    Id = entity.UserId,
                    UserName = $"User_{entity.UserId}",
                    Email = $"user{entity.UserId}@example.com"
                };

                // add new user to context and save
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // update TimeBlock
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}