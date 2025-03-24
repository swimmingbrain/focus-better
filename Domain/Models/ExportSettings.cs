using MonkMode.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace MonkMode.Domain.Models
{
    public class ExportSettings : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CalendarProvider { get; set; }
        public string ExternalCalendarId { get; set; }
        public SyncFrequency SyncFrequency { get; set; }
        public DateTime? LastSyncDate { get; set; }

        // navigation properties
        public User User { get; set; }
        public List<CalendarEvent> Events { get; set; }
    }

    public enum SyncFrequency
    {
        MANUAL,
        HOURLY,
        DAILY,
        WEEKLY
    }
}