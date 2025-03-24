using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface IExportService
    {
        Task<ExportResult> SynchronizeCalendarAsync(int userId);
        Task<List<ExportableEvent>> GetExportableEventsAsync(int userId, DateTime startDate, DateTime endDate);
        Task<CalendarFileResult> GenerateCalendarFileAsync(int userId, string format, DateTime startDate, DateTime endDate);
        Task<ServiceResult<bool>> DisconnectCalendarAsync(int userId);
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int EventsExported { get; set; }
    }

    public class ExportableEvent
    {
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsTask { get; set; }
        public bool IsFocusSession { get; set; }
        public int SourceId { get; set; }
        public string SourceType { get; set; }
    }

    public class CalendarFileResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public byte[] FileContent { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}