using MonkMode.Domain.Repositories;
using System;
using MonkMode.Domain.Enums;

namespace MonkMode.Domain.Models
{
    public class FocusSession : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public FocusMode Mode { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; } // in minutes

        // navigation properties
        public User User { get; set; }
    }
}