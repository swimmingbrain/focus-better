using MonkMode.Domain.Repositories;
using System;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class RecurringTask : IEntity
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public RecurrencePattern Pattern { get; set; }
        public int Interval { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // navigation properties
        public TaskItem Task { get; set; }
    }

    public enum RecurrencePattern
    {
        DAILY,
        WEEKLY,
        MONTHLY,
        YEARLY
    }
}