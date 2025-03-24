// In TaskItem.cs
using MonkMode.Domain.Repositories;
using System;
using System.Collections.Generic;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Models
{
    public class TaskItem : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskItemStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // navigation properties
        public User User { get; set; }
        public RecurringTask RecurringConfiguration { get; set; }
        public List<TimeBlock> LinkedTimeBlocks { get; set; }
    }
}