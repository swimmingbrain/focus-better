using MonkMode.Domain.Models;
using MonkMode.Domain.Services;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface ITaskService
    {
        Task<ServiceResult<TaskItem>> CreateTaskAsync(TaskItem task);
        Task<ServiceResult<TaskItem>> UpdateTaskAsync(TaskItem task);
        Task<ServiceResult<bool>> LinkTaskToTimeBlockAsync(int taskId, int timeBlockId, int userId);
        Task<ServiceResult<bool>> UnlinkTaskFromTimeBlockAsync(int taskId, int timeBlockId, int userId);
        Task ScheduleTaskReminderAsync(TaskItem task);
        Task UpdateTaskReminderAsync(TaskItem task);
        Task<ServiceResult<TaskItem>> CreateNextRecurringTaskAsync(TaskItem completedTask);
    }
}