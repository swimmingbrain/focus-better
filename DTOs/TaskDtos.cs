using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace MonkMode.DTOs
{
    public class TaskItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsRecurring { get; set; }
    }

    public class TaskDetailDto : TaskItemDto
    {
        public RecurringTaskDto RecurringConfiguration { get; set; }
        public List<TimeBlockDto> LinkedTimeBlocks { get; set; }
    }

    public class CreateTaskDto
    {
        [DebuggerDisplay("{Title}, RecurringConfig: {RecurringConfiguration != null}")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public string Priority { get; set; }

        public DateTime? DueDate { get; set; }

        // This is the problematic field
        public CreateRecurringTaskDto RecurringConfiguration { get; set; }
    }

    public class UpdateTaskDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime? DueDate { get; set; }
        public UpdateRecurringTaskDto RecurringConfiguration { get; set; }
    }

    public class RecurringTaskDto
    {
        public string Pattern { get; set; }
        public int Interval { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateRecurringTaskDto
    {
        [Required]
        public string Pattern { get; set; }

        [Range(1, 365)]
        public int Interval { get; set; } = 1;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class UpdateRecurringTaskDto
    {
        public string Pattern { get; set; }

        [Range(1, 365)]
        public int Interval { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class TaskFilterDto
    {
        public string Status { get; set; }
        public string Priority { get; set; }
        public bool? CompletedOnly { get; set; }
        public bool? IncludeCompleted { get; set; }
        public DateTime? DueDateStart { get; set; }
        public DateTime? DueDateEnd { get; set; }
        public bool? IsRecurring { get; set; }
    }
}