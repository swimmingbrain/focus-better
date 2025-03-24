using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonkMode.DTOs
{
    public class TimeBlockDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Color { get; set; }
        public List<int> LinkedTaskIds { get; set; }
    }

    public class TimeBlockDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Color { get; set; }
        public List<TaskItemDto> LinkedTasks { get; set; }
    }

    public class CreateTimeBlockDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Title { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string Color { get; set; }

        public List<int> TaskIds { get; set; }
    }

    public class UpdateTimeBlockDto
    {
        public string Title { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Color { get; set; }
    }
}