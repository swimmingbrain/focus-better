using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        private readonly IExportRepository _exportRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITimeBlockRepository _timeBlockRepository;
        private readonly IFocusSessionRepository _focusSessionRepository;

        public ExportService(
            IExportRepository exportRepository,
            ITaskRepository taskRepository,
            ITimeBlockRepository timeBlockRepository,
            IFocusSessionRepository focusSessionRepository)
        {
            _exportRepository = exportRepository;
            _taskRepository = taskRepository;
            _timeBlockRepository = timeBlockRepository;
            _focusSessionRepository = focusSessionRepository;
        }

        public async Task<ExportResult> SynchronizeCalendarAsync(int userId)
        {
            try
            {
                // get export settings for the user
                var settings = await _exportRepository.GetSettingsForUserAsync(userId);
                if (settings == null)
                {
                    return new ExportResult
                    {
                        Success = false,
                        Message = "Export settings not found",
                        EventsExported = 0
                    };
                }

                // start date is today, end date is 30 days from now
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(30);

                var events = await GetExportableEventsAsync(userId, startDate, endDate);

                // nur SIMULATION -- keine Implementierung

                // simulate creating events in external calendar
                var eventCount = events.Count;

                // update last sync date
                settings.LastSyncDate = DateTime.UtcNow;
                await _exportRepository.UpdateAsync(settings);

                return new ExportResult
                {
                    Success = true,
                    Message = $"Successfully exported {eventCount} events to calendar",
                    EventsExported = eventCount
                };
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    Message = $"Failed to synchronize calendar: {ex.Message}",
                    EventsExported = 0
                };
            }
        }

        public async Task<List<ExportableEvent>> GetExportableEventsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var events = new List<ExportableEvent>();

            // get time blocks
            var timeBlocks = await _timeBlockRepository.GetTimeBlocksForUserAsync(userId, startDate, endDate);
            foreach (var timeBlock in timeBlocks)
            {
                events.Add(new ExportableEvent
                {
                    Title = timeBlock.Title,
                    StartTime = timeBlock.StartTime,
                    EndTime = timeBlock.EndTime,
                    IsTask = false,
                    IsFocusSession = false,
                    SourceId = timeBlock.Id,
                    SourceType = "TimeBlock"
                });
            }

            // get tasks with due dates in the range
            var tasks = await _taskRepository.GetDueTasksAsync(startDate, endDate);
            foreach (var task in tasks)
            {
                if (task.DueDate.HasValue && task.UserId == userId)
                {
                    // create a 1-hour event at the due date
                    var dueDate = task.DueDate.Value;
                    events.Add(new ExportableEvent
                    {
                        Title = $"Due: {task.Title}",
                        StartTime = dueDate,
                        EndTime = dueDate.AddHours(1),
                        IsTask = true,
                        IsFocusSession = false,
                        SourceId = task.Id,
                        SourceType = "Task"
                    });
                }
            }

            var focusSessions = await _focusSessionRepository.GetFocusSessionsForUserAsync(userId, startDate, endDate);
            foreach (var session in focusSessions)
            {
                if (session.EndTime.HasValue)
                {
                    events.Add(new ExportableEvent
                    {
                        Title = $"Focus: {session.Mode}",
                        StartTime = session.StartTime,
                        EndTime = session.EndTime.Value,
                        IsTask = false,
                        IsFocusSession = true,
                        SourceId = session.Id,
                        SourceType = "FocusSession"
                    });
                }
            }

            return events;
        }

        public async Task<CalendarFileResult> GenerateCalendarFileAsync(int userId, string format, DateTime startDate, DateTime endDate)
        {
            try
            {
                // get events to export
                var events = await GetExportableEventsAsync(userId, startDate, endDate);

                if (events.Count == 0)
                {
                    return new CalendarFileResult
                    {
                        Success = false,
                        Message = "No events to export",
                        FileContent = null,
                        ContentType = null,
                        FileName = null
                    };
                }

                // generate iCalendar file
                var icsContent = GenerateICalendar(events);
                var fileContent = Encoding.UTF8.GetBytes(icsContent);

                return new CalendarFileResult
                {
                    Success = true,
                    Message = "Calendar file generated successfully",
                    FileContent = fileContent,
                    ContentType = "text/calendar",
                    FileName = $"productivity-calendar-{DateTime.UtcNow:yyyyMMdd}.ics"
                };
            }
            catch (Exception ex)
            {
                return new CalendarFileResult
                {
                    Success = false,
                    Message = $"Failed to generate calendar file: {ex.Message}",
                    FileContent = null,
                    ContentType = null,
                    FileName = null
                };
            }
        }

        private string GenerateICalendar(List<ExportableEvent> events)
        {
            var sb = new StringBuilder();

            // calendar header
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//MonkMode//EN");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");

            // add events
            foreach (var evt in events)
            {
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:{Guid.NewGuid()}");
                sb.AppendLine($"SUMMARY:{evt.Title}");
                sb.AppendLine($"DTSTART:{FormatDateTimeForICalendar(evt.StartTime)}");
                sb.AppendLine($"DTEND:{FormatDateTimeForICalendar(evt.EndTime)}");
                sb.AppendLine($"DTSTAMP:{FormatDateTimeForICalendar(DateTime.UtcNow)}");
                sb.AppendLine($"CREATED:{FormatDateTimeForICalendar(DateTime.UtcNow)}");

                // add category based on source type
                string category;
                if (evt.IsTask) category = "Task";
                else if (evt.IsFocusSession) category = "Focus Session";
                else category = "Time Block";

                sb.AppendLine($"CATEGORIES:{category}");

                sb.AppendLine("END:VEVENT");
            }

            // calendar footer
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }

        private string FormatDateTimeForICalendar(DateTime dateTime)
        {
            // format as YYYYMMDDTHHmmssZ UTC time
            return dateTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        }

        public async Task<ServiceResult<bool>> DisconnectCalendarAsync(int userId)
        {
            try
            {
                var settings = await _exportRepository.GetSettingsForUserAsync(userId);

                if (settings == null)
                {
                    return ServiceResult<bool>.CreateSuccess(true, "No settings to delete");
                }

                await _exportRepository.DeleteAllEventsAsync(settings.Id);

                await _exportRepository.DeleteAsync(settings);

                return ServiceResult<bool>.CreateSuccess(true, "Calendar disconnected successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.CreateError($"Failed to disconnect calendar: {ex.Message}");
            }
        }
    }
}