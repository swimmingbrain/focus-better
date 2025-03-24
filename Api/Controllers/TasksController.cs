using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.DTOs;
using MonkMode.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskService _taskService;
        private readonly INotificationService _notificationService;

        public TasksController(
            ITaskRepository taskRepository,
            ITaskService taskService,
            INotificationService notificationService)
        {
            _taskRepository = taskRepository;
            _taskService = taskService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetTasks([FromQuery] TaskFilterDto filter)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var tasks = await _taskRepository.GetTasksForUserAsync(userId, filter);

            var taskDtos = tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                DueDate = t.DueDate,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt,
                IsRecurring = t.RecurringConfiguration != null
            }).ToList();

            return taskDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDetailDto>> GetTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var task = await _taskRepository.GetTaskWithDetailsAsync(id);

            if (task == null)
                return NotFound();

            // ensure user owns the task
            if (task.UserId != userId)
                return Forbid();

            var recurringConfig = task.RecurringConfiguration;
            RecurringTaskDto recurringDto = null;

            if (recurringConfig != null)
            {
                recurringDto = new RecurringTaskDto
                {
                    Pattern = recurringConfig.Pattern.ToString(),
                    Interval = recurringConfig.Interval,
                    StartDate = recurringConfig.StartDate,
                    EndDate = recurringConfig.EndDate
                };
            }

            return new TaskDetailDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                RecurringConfiguration = recurringDto,
                LinkedTimeBlocks = task.LinkedTimeBlocks?.Select(tb => new TimeBlockDto
                {
                    Id = tb.Id,
                    Title = tb.Title,
                    StartTime = tb.StartTime,
                    EndTime = tb.EndTime,
                    Color = tb.Color
                }).ToList()
            };
        }

        [HttpPost]
        public async Task<ActionResult<TaskItemDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                // clear model state error for RecurringConfiguration if it exists
                if (!ModelState.IsValid && ModelState.ContainsKey("RecurringConfiguration"))
                {
                    ModelState.Remove("RecurringConfiguration");

                    // if still other errors after removing RecurringConfiguration, return them
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // parse enums
                if (!Enum.TryParse<TaskPriority>(createTaskDto.Priority, true, out var priority))
                {
                    return BadRequest($"Invalid priority: {createTaskDto.Priority}");
                }

                var task = new TaskItem
                {
                    UserId = userId,
                    Title = createTaskDto.Title,
                    Description = createTaskDto.Description,
                    Priority = priority,
                    Status = TaskItemStatus.TODO,
                    DueDate = createTaskDto.DueDate,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                // handle RecurringConfiguration
                if (createTaskDto.RecurringConfiguration != null)
                {
                    if (!Enum.TryParse<RecurrencePattern>(createTaskDto.RecurringConfiguration.Pattern, true, out var pattern))
                    {
                        return BadRequest($"Invalid recurrence pattern: {createTaskDto.RecurringConfiguration.Pattern}");
                    }

                    task.RecurringConfiguration = new RecurringTask
                    {
                        Pattern = pattern,
                        Interval = createTaskDto.RecurringConfiguration.Interval,
                        StartDate = createTaskDto.RecurringConfiguration.StartDate ?? DateTime.UtcNow,
                        EndDate = createTaskDto.RecurringConfiguration.EndDate
                    };
                }

                var createdTask = await _taskRepository.AddAsync(task);

                // schedule reminder notifications if due date is fix
                if (task.DueDate.HasValue)
                {
                    await _taskService.ScheduleTaskReminderAsync(task);
                }

                return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, new TaskItemDto
                {
                    Id = createdTask.Id,
                    Title = createdTask.Title,
                    Description = createdTask.Description,
                    Priority = createdTask.Priority.ToString(),
                    Status = createdTask.Status.ToString(),
                    DueDate = createdTask.DueDate,
                    IsCompleted = createdTask.IsCompleted,
                    CreatedAt = createdTask.CreatedAt,
                    IsRecurring = createdTask.RecurringConfiguration != null
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating task: " + ex.ToString());
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var task = await _taskRepository.GetTaskWithDetailsAsync(id);

            if (task == null)
                return NotFound();

            // make sure task gehört zu user
            if (task.UserId != userId)
                return Forbid();

            // update task fields
            if (!string.IsNullOrEmpty(updateTaskDto.Title))
                task.Title = updateTaskDto.Title;

            if (updateTaskDto.Description != null)
                task.Description = updateTaskDto.Description;

            if (!string.IsNullOrEmpty(updateTaskDto.Priority) &&
                Enum.TryParse<TaskPriority>(updateTaskDto.Priority, true, out var priority))
            {
                task.Priority = priority;
            }

            if (!string.IsNullOrEmpty(updateTaskDto.Status) &&
                Enum.TryParse<TaskItemStatus>(updateTaskDto.Status, true, out var status))
            {
                var oldStatus = task.Status;
                task.Status = status;

                // if task is being marked as completed
                if (oldStatus != TaskItemStatus.COMPLETED && status == TaskItemStatus.COMPLETED)
                {
                    task.IsCompleted = true;
                    task.CompletedAt = DateTime.UtcNow;

                    // if this is a recurring task, schedule next task
                    if (task.RecurringConfiguration != null)
                    {
                        await _taskService.CreateNextRecurringTaskAsync(task);
                    }
                }
                // if task is being unmarked as completed
                else if (oldStatus == TaskItemStatus.COMPLETED && status != TaskItemStatus.COMPLETED)
                {
                    task.IsCompleted = false;
                    task.CompletedAt = null;
                }
            }

            if (updateTaskDto.DueDate.HasValue)
            {
                var oldDueDate = task.DueDate;
                task.DueDate = updateTaskDto.DueDate;

                // if due date changed, update reminders
                if (oldDueDate != updateTaskDto.DueDate)
                {
                    await _taskService.UpdateTaskReminderAsync(task);
                }
            }

            // update configuration
            if (updateTaskDto.RecurringConfiguration != null)
            {
                if (task.RecurringConfiguration == null)
                {
                    if (Enum.TryParse<RecurrencePattern>(updateTaskDto.RecurringConfiguration.Pattern, true, out var pattern))
                    {
                        task.RecurringConfiguration = new RecurringTask
                        {
                            TaskId = task.Id,
                            Pattern = pattern,
                            Interval = updateTaskDto.RecurringConfiguration.Interval,
                            StartDate = updateTaskDto.RecurringConfiguration.StartDate ?? DateTime.UtcNow,
                            EndDate = updateTaskDto.RecurringConfiguration.EndDate
                        };
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(updateTaskDto.RecurringConfiguration.Pattern) &&
                        Enum.TryParse<RecurrencePattern>(updateTaskDto.RecurringConfiguration.Pattern, true, out var pattern))
                    {
                        task.RecurringConfiguration.Pattern = pattern;
                    }

                    if (updateTaskDto.RecurringConfiguration.Interval > 0)
                    {
                        task.RecurringConfiguration.Interval = updateTaskDto.RecurringConfiguration.Interval;
                    }

                    if (updateTaskDto.RecurringConfiguration.StartDate.HasValue)
                    {
                        task.RecurringConfiguration.StartDate = updateTaskDto.RecurringConfiguration.StartDate.Value;
                    }

                    task.RecurringConfiguration.EndDate = updateTaskDto.RecurringConfiguration.EndDate;
                }
            }

            await _taskRepository.UpdateAsync(task);

            return new TaskItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                IsRecurring = task.RecurringConfiguration != null
            };
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var task = await _taskRepository.GetTaskWithDetailsAsync(id);

            if (task == null)
                return NotFound();

            // make sure task gehört zu user
            if (task.UserId != userId)
                return Forbid();

            await _taskRepository.DeleteAsync(task);
            return NoContent();
        }

        [HttpPost("{id}/link-timeblock/{timeBlockId}")]
        public async Task<ActionResult> LinkTaskToTimeBlock(int id, int timeBlockId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _taskService.LinkTaskToTimeBlockAsync(id, timeBlockId, userId);

            if (!result.Success)
                return result.ErrorType switch
                {
                    "NotFound" => NotFound(result.Message),
                    "Unauthorized" => Forbid(),
                    _ => BadRequest(result.Message)
                };

            return NoContent();
        }

        [HttpDelete("{id}/unlink-timeblock/{timeBlockId}")]
        public async Task<ActionResult> UnlinkTaskFromTimeBlock(int id, int timeBlockId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _taskService.UnlinkTaskFromTimeBlockAsync(id, timeBlockId, userId);

            if (!result.Success)
                return result.ErrorType switch
                {
                    "NotFound" => NotFound(result.Message),
                    "Unauthorized" => Forbid(),
                    _ => BadRequest(result.Message)
                };

            return NoContent();
        }
    }
}