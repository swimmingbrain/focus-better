using MonkMode.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class TimeBlock : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Color { get; set; }

        // navigation properties
        public User User { get; set; }
        public List<TaskItem> LinkedTasks { get; set; }
    }
}