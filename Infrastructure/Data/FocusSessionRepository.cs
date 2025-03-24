using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Data
{
    public class FocusSessionRepository : IFocusSessionRepository
    {
        private readonly AppDbContext _context;

        public FocusSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FocusSession> AddAsync(FocusSession entity)
        {
            await _context.FocusSessions.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(FocusSession entity)
        {
            _context.FocusSessions.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FocusSession>> FindAllAsync()
        {
            return await _context.FocusSessions.ToListAsync();
        }

        public async Task<FocusSession> FindByIdAsync(int id)
        {
            return await _context.FocusSessions.FindAsync(id);
        }

        public async Task<FocusSession> GetActiveSessionForUserAsync(int userId)
        {
            return await _context.FocusSessions
                .Where(fs => fs.UserId == userId && fs.EndTime == null)
                .OrderByDescending(fs => fs.StartTime)
                .FirstOrDefaultAsync();
        }

        public async Task<List<FocusSession>> GetFocusSessionsForUserAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.FocusSessions
                .Where(fs => fs.UserId == userId &&
                           ((fs.StartTime >= startDate && fs.StartTime <= endDate) ||
                            (fs.EndTime >= startDate && fs.EndTime <= endDate) ||
                            (fs.StartTime <= startDate && fs.EndTime >= endDate) ||
                            (fs.StartTime <= startDate && fs.EndTime == null)))
                .OrderByDescending(fs => fs.StartTime)
                .ToListAsync();
        }

        public async Task<FocusSession> UpdateAsync(FocusSession entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}