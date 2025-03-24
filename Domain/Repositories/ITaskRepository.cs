using MonkMode.Domain.Models;
using MonkMode.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface ITaskRepository : IRepository<TaskItem>
    {
        Task<List<TaskItem>> GetTasksForUserAsync(int userId, TaskFilterDto filter = null);
        Task<TaskItem> GetTaskWithDetailsAsync(int taskId);
        Task<List<TaskItem>> GetDueTasksAsync(DateTime startDate, DateTime endDate);
        Task<List<TaskItem>> GetTasksForTimeBlockAsync(int timeBlockId);
    }
}