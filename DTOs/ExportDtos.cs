using System;
using System.ComponentModel.DataAnnotations;

namespace MonkMode.DTOs
{
    public class ExportSettingsDto
    {
        public int Id { get; set; }
        public string CalendarProvider { get; set; }
        public string ExternalCalendarId { get; set; }
        public string SyncFrequency { get; set; }
        public DateTime? LastSyncDate { get; set; }
    }

    public class UpdateExportSettingsDto
    {
        [Required]
        public string CalendarProvider { get; set; }

        public string ExternalCalendarId { get; set; }

        [Required]
        public string SyncFrequency { get; set; }
    }

    public class ExportResultDto
    {
        public bool Success { get; set; }
        public int EventsExported { get; set; }
        public string Message { get; set; }
    }

    public class CalendarEventDto
    {
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsTask { get; set; }
        public bool IsFocusSession { get; set; }
    }

    public class CalendarProviderDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool SupportsAutoSync { get; set; }
    }
}