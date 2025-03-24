using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.DTOs;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TimeBlocksController : ControllerBase
    {
        private readonly ITimeBlockRepository _timeBlockRepository;
        private readonly ITaskRepository _taskRepository;

        public TimeBlocksController(
            ITimeBlockRepository timeBlockRepository,
            ITaskRepository taskRepository)
        {
            _timeBlockRepository = timeBlockRepository;
            _taskRepository = taskRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeBlockDto>>> GetTimeBlocks(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var timeBlocks = await _timeBlockRepository.GetTimeBlocksForUserAsync(
                userId,
                startDate ?? DateTime.UtcNow.Date.AddDays(-7),
                endDate ?? DateTime.UtcNow.Date.AddDays(30)
            );

            var timeBlockDtos = timeBlocks.Select(tb => new TimeBlockDto
            {
                Id = tb.Id,
                Title = tb.Title,
                StartTime = tb.StartTime,
                EndTime = tb.EndTime,
                Color = tb.Color,
                LinkedTaskIds = tb.LinkedTasks?.Select(t => t.Id).ToList()
            }).ToList();

            return timeBlockDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeBlockDetailDto>> GetTimeBlock(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var timeBlock = await _timeBlockRepository.GetTimeBlockWithTasksAsync(id);

            if (timeBlock == null)
                return NotFound();

            // Ensure user owns the time block
            if (timeBlock.UserId != userId)
                return Forbid();

            return new TimeBlockDetailDto
            {
                Id = timeBlock.Id,
                Title = timeBlock.Title,
                StartTime = timeBlock.StartTime,
                EndTime = timeBlock.EndTime,
                Color = timeBlock.Color,
                LinkedTasks = timeBlock.LinkedTasks?.Select(t => new TaskItemDto
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
                }).ToList()
            };
        }

        [HttpPost]
        public async Task<ActionResult<TimeBlockDto>> CreateTimeBlock(CreateTimeBlockDto createTimeBlockDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // validate time range
            if (createTimeBlockDto.EndTime <= createTimeBlockDto.StartTime)
            {
                return BadRequest("End time must be after start time");
            }

            var timeBlock = new TimeBlock
            {
                UserId = userId,
                Title = createTimeBlockDto.Title,
                StartTime = createTimeBlockDto.StartTime,
                EndTime = createTimeBlockDto.EndTime,
                Color = createTimeBlockDto.Color ?? "#3498db" // Default blue color
            };

            var createdTimeBlock = await _timeBlockRepository.AddAsync(timeBlock);

            // link tasks
            if (createTimeBlockDto.TaskIds != null && createTimeBlockDto.TaskIds.Any())
            {
                foreach (var taskId in createTimeBlockDto.TaskIds)
                {
                    var task = await _taskRepository.FindByIdAsync(taskId);
                    if (task != null && task.UserId == userId)
                    {
                        if (timeBlock.LinkedTasks == null)
                            timeBlock.LinkedTasks = new List<TaskItem>();

                        timeBlock.LinkedTasks.Add(task);
                    }
                }

                await _timeBlockRepository.UpdateAsync(timeBlock);
            }

            return CreatedAtAction(nameof(GetTimeBlock), new { id = createdTimeBlock.Id }, new TimeBlockDto
            {
                Id = createdTimeBlock.Id,
                Title = createdTimeBlock.Title,
                StartTime = createdTimeBlock.StartTime,
                EndTime = createdTimeBlock.EndTime,
                Color = createdTimeBlock.Color,
                LinkedTaskIds = createdTimeBlock.LinkedTasks?.Select(t => t.Id).ToList()
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TimeBlockDto>> UpdateTimeBlock(int id, UpdateTimeBlockDto updateTimeBlockDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var timeBlock = await _timeBlockRepository.GetTimeBlockWithTasksAsync(id);

            if (timeBlock == null)
                return NotFound();

            // make sure time block gehört zu user
            if (timeBlock.UserId != userId)
                return Forbid();

            // update time block fields
            if (!string.IsNullOrEmpty(updateTimeBlockDto.Title))
                timeBlock.Title = updateTimeBlockDto.Title;

            if (updateTimeBlockDto.StartTime.HasValue)
                timeBlock.StartTime = updateTimeBlockDto.StartTime.Value;

            if (updateTimeBlockDto.EndTime.HasValue)
                timeBlock.EndTime = updateTimeBlockDto.EndTime.Value;

            // validate time range
            if (timeBlock.EndTime <= timeBlock.StartTime)
            {
                return BadRequest("End time must be after start time");
            }

            if (!string.IsNullOrEmpty(updateTimeBlockDto.Color))
                timeBlock.Color = updateTimeBlockDto.Color;

            await _timeBlockRepository.UpdateAsync(timeBlock);

            return new TimeBlockDto
            {
                Id = timeBlock.Id,
                Title = timeBlock.Title,
                StartTime = timeBlock.StartTime,
                EndTime = timeBlock.EndTime,
                Color = timeBlock.Color,
                LinkedTaskIds = timeBlock.LinkedTasks?.Select(t => t.Id).ToList()
            };
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTimeBlock(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var timeBlock = await _timeBlockRepository.FindByIdAsync(id);

            if (timeBlock == null)
                return NotFound();

            // make sure time block gehört zu user
            if (timeBlock.UserId != userId)
                return Forbid();

            await _timeBlockRepository.DeleteAsync(timeBlock);
            return NoContent();
        }

        [HttpGet("overlap")]
        public async Task<ActionResult<bool>> CheckTimeBlockOverlap(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] int? excludeId)
        {
            if (endTime <= startTime)
            {
                return BadRequest("End time must be after start time");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var hasOverlap = await _timeBlockRepository.HasOverlappingTimeBlocksAsync(
                userId, startTime, endTime, excludeId);

            return hasOverlap;
        }
    }
}