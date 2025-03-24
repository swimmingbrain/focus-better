using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITimeBlockRepository _timeBlockRepository;
        private readonly INotificationService _notificationService;

        public TaskService(
            ITaskRepository taskRepository,
            ITimeBlockRepository timeBlockRepository,
            INotificationService notificationService)
        {
            _taskRepository = taskRepository;
            _timeBlockRepository = timeBlockRepository;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<TaskItem>> CreateTaskAsync(TaskItem task)
        {
            try
            {
                task.Status = TaskItemStatus.TODO;

                var createdTask = await _taskRepository.AddAsync(task);
                return ServiceResult<TaskItem>.CreateSuccess(createdTask);
            }
            catch (Exception ex)
            {
                return ServiceResult<TaskItem>.CreateError($"Failed to create task: {ex.Message}");
            }
        }

        public async Task<ServiceResult<TaskItem>> UpdateTaskAsync(TaskItem task)
        {
            try
            {
                var updatedTask = await _taskRepository.UpdateAsync(task);
                return ServiceResult<TaskItem>.CreateSuccess(updatedTask);
            }
            catch (Exception ex)
            {
                return ServiceResult<TaskItem>.CreateError($"Failed to update task: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> LinkTaskToTimeBlockAsync(
            int taskId,
            int timeBlockId,
            int userId)
        {
            try
            {
                var task = await _taskRepository.GetTaskWithDetailsAsync(taskId);
                if (task == null)
                {
                    return ServiceResult<bool>.CreateError("Task not found", "NotFound");
                }

                var timeBlock = await _timeBlockRepository.GetTimeBlockWithTasksAsync(timeBlockId);
                if (timeBlock == null)
                {
                    return ServiceResult<bool>.CreateError("Time block not found", "NotFound");
                }

                // verify ownership
                if (task.UserId != userId || timeBlock.UserId != userId)
                {
                    return ServiceResult<bool>.CreateError("You don't have permission to link these items", "Unauthorized");
                }

                if (task.LinkedTimeBlocks == null)
                {
                    task.LinkedTimeBlocks = new List<TimeBlock>();
                }

                if (!task.LinkedTimeBlocks.Any(tb => tb.Id == timeBlockId))
                {
                    task.LinkedTimeBlocks.Add(timeBlock);
                    await _taskRepository.UpdateAsync(task);
                }

                return ServiceResult<bool>.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.CreateError($"Failed to link task to time block: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UnlinkTaskFromTimeBlockAsync(
            int taskId,
            int timeBlockId,
            int userId)
        {
            try
            {
                var task = await _taskRepository.GetTaskWithDetailsAsync(taskId);
                if (task == null)
                {
                    return ServiceResult<bool>.CreateError("Task not found", "NotFound");
                }

                var timeBlock = await _timeBlockRepository.FindByIdAsync(timeBlockId);
                if (timeBlock == null)
                {
                    return ServiceResult<bool>.CreateError("Time block not found", "NotFound");
                }

                // verify ownership
                if (task.UserId != userId || timeBlock.UserId != userId)
                {
                    return ServiceResult<bool>.CreateError("You don't have permission to unlink these items", "Unauthorized");
                }

                if (task.LinkedTimeBlocks != null && task.LinkedTimeBlocks.Any(tb => tb.Id == timeBlockId))
                {
                    task.LinkedTimeBlocks.RemoveAll(tb => tb.Id == timeBlockId);
                    await _taskRepository.UpdateAsync(task);
                }

                return ServiceResult<bool>.CreateSuccess(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.CreateError($"Failed to unlink task from time block: {ex.Message}");
            }
        }

        public async Task ScheduleTaskReminderAsync(TaskItem task)
        {
            if (task.DueDate.HasValue)
            {
                // calc reminder time
                var reminderTime = task.DueDate.Value.AddDays(-1);

                // if due date is approaching soon (24 hrs), send notification immediately
                if (task.DueDate.Value < DateTime.UtcNow.AddHours(24))
                {
                    await SendTaskReminderAsync(task.UserId, task.Id);
                }
            }
        }

        public async Task UpdateTaskReminderAsync(TaskItem task)
        {
            // schedule new reminder
            await ScheduleTaskReminderAsync(task);
        }

        private async Task SendTaskReminderAsync(int userId, int taskId)
        {
            var task = await _taskRepository.FindByIdAsync(taskId);
            if (task != null && !task.IsCompleted)
            {
                await _notificationService.CreateTaskReminderNotificationAsync(userId, task);
            }
        }

        public async Task<ServiceResult<TaskItem>> CreateNextRecurringTaskAsync(TaskItem completedTask)
        {
            try
            {
                if (completedTask.RecurringConfiguration == null)
                {
                    return ServiceResult<TaskItem>.CreateError("Task is not recurring");
                }

                var config = completedTask.RecurringConfiguration;

                if (config.EndDate.HasValue && DateTime.UtcNow > config.EndDate.Value)
                {
                    return ServiceResult<TaskItem>.CreateError("Recurrence end date has passed");
                }

                // calc next due date based on recurrence pattern
                DateTime? nextDueDate = null;
                if (completedTask.DueDate.HasValue)
                {
                    nextDueDate = completedTask.DueDate.Value;

                    switch (config.Pattern)
                    {
                        case RecurrencePattern.DAILY:
                            nextDueDate = nextDueDate.Value.AddDays(config.Interval);
                            break;
                        case RecurrencePattern.WEEKLY:
                            nextDueDate = nextDueDate.Value.AddDays(7 * config.Interval);
                            break;
                        case RecurrencePattern.MONTHLY:
                            nextDueDate = nextDueDate.Value.AddMonths(config.Interval);
                            break;
                        case RecurrencePattern.YEARLY:
                            nextDueDate = nextDueDate.Value.AddYears(config.Interval);
                            break;
                    }
                }

                // create new task with the same properties
                var newTask = new TaskItem
                {
                    UserId = completedTask.UserId,
                    Title = completedTask.Title,
                    Description = completedTask.Description,
                    Priority = completedTask.Priority,
                    Status = TaskItemStatus.TODO,
                    DueDate = nextDueDate,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                // add recurring configuration to new task
                newTask.RecurringConfiguration = new RecurringTask
                {
                    Pattern = config.Pattern,
                    Interval = config.Interval,
                    StartDate = config.StartDate,
                    EndDate = config.EndDate
                };

                var createdTask = await _taskRepository.AddAsync(newTask);

                // schedule reminder for this new task
                if (nextDueDate.HasValue)
                {
                    await ScheduleTaskReminderAsync(createdTask);
                }

                return ServiceResult<TaskItem>.CreateSuccess(createdTask);
            }
            catch (Exception ex)
            {
                return ServiceResult<TaskItem>.CreateError($"Failed to create next recurring task: {ex.Message}");
            }
        }
    }
}