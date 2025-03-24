using MonkMode.Domain.Repositories;
using System;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class CalendarEvent : IEntity
    {
        public int Id { get; set; }
        public int ExportSettingsId { get; set; }
        public string ExternalEventId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // navigation properties
        public ExportSettings Settings { get; set; }
    }
}