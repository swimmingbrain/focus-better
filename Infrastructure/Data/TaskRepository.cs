using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.DTOs;
using MonkMode.Domain.Enums;
using MonkMode.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Data
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> AddAsync(TaskItem entity)
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

            await _context.Tasks.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(TaskItem entity)
        {
            _context.Tasks.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TaskItem>> FindAllAsync()
        {
            return await _context.Tasks.ToListAsync();
        }

        public async Task<TaskItem> FindByIdAsync(int id)
        {
            return await _context.Tasks.FindAsync(id);
        }

        public async Task<List<TaskItem>> GetDueTasksAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Tasks
                .Include(t => t.User)
                .Include(t => t.RecurringConfiguration)
                .Where(t => t.DueDate.HasValue &&
                          t.DueDate >= startDate &&
                          t.DueDate <= endDate &&
                          !t.IsCompleted)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksForTimeBlockAsync(int timeBlockId)
        {
            return await _context.TimeBlocks
                .Where(tb => tb.Id == timeBlockId)
                .SelectMany(tb => tb.LinkedTasks)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksForUserAsync(int userId, TaskFilterDto filter = null)
        {
            IQueryable<TaskItem> query = _context.Tasks
                .Include(t => t.RecurringConfiguration)
                .Where(t => t.UserId == userId);

            if (filter != null)
            {
                // filter by status
                if (!string.IsNullOrEmpty(filter.Status) &&
                    Enum.TryParse<TaskItemStatus>(filter.Status, true, out var status))
                {
                    query = query.Where(t => t.Status == status);
                }

                // filter by priority
                if (!string.IsNullOrEmpty(filter.Priority) &&
                    Enum.TryParse<TaskPriority>(filter.Priority, true, out var priority))
                {
                    query = query.Where(t => t.Priority == priority);
                }

                // filter by completion status
                if (filter.CompletedOnly.HasValue && filter.CompletedOnly.Value)
                {
                    query = query.Where(t => t.IsCompleted);
                }
                else if (filter.IncludeCompleted.HasValue && !filter.IncludeCompleted.Value)
                {
                    query = query.Where(t => !t.IsCompleted);
                }

                // filter by due date range
                if (filter.DueDateStart.HasValue)
                {
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate >= filter.DueDateStart.Value);
                }

                if (filter.DueDateEnd.HasValue)
                {
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate <= filter.DueDateEnd.Value);
                }

                // filter by recurring status
                if (filter.IsRecurring.HasValue)
                {
                    if (filter.IsRecurring.Value)
                    {
                        query = query.Where(t => t.RecurringConfiguration != null);
                    }
                    else
                    {
                        query = query.Where(t => t.RecurringConfiguration == null);
                    }
                }
            }

            return await query.ToListAsync();
        }

        public async Task<TaskItem> GetTaskWithDetailsAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.RecurringConfiguration)
                .Include(t => t.LinkedTimeBlocks)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<TaskItem> UpdateAsync(TaskItem entity)
        {
            // check if user exists by UserId
            var user = await _context.Users.FindAsync(entity.UserId);

            // if not create new
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

            _context.Entry(entity).State = EntityState.Modified;

            if (entity.RecurringConfiguration != null)
            {
                if (entity.RecurringConfiguration.Id == 0)
                {
                    _context.RecurringTasks.Add(entity.RecurringConfiguration);
                }
                else
                {
                    _context.Entry(entity.RecurringConfiguration).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();
            return entity;
        }
    }
}