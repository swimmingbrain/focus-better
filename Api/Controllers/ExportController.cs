using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.DTOs;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IExportRepository _exportRepository;
        private readonly IExportService _exportService;

        public ExportController(
            IExportRepository exportRepository,
            IExportService exportService)
        {
            _exportRepository = exportRepository;
            _exportService = exportService;
        }

        [HttpGet("settings")]
        public async Task<ActionResult<ExportSettingsDto>> GetExportSettings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var settings = await _exportRepository.GetSettingsForUserAsync(userId);

            if (settings == null)
                return NoContent(); // No settings yet

            return new ExportSettingsDto
            {
                Id = settings.Id,
                CalendarProvider = settings.CalendarProvider,
                ExternalCalendarId = settings.ExternalCalendarId,
                SyncFrequency = settings.SyncFrequency.ToString(),
                LastSyncDate = settings.LastSyncDate
            };
        }

        [HttpPost("settings")]
        public async Task<ActionResult<ExportSettingsDto>> SaveExportSettings(UpdateExportSettingsDto settingsDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // parse enum
            if (!Enum.TryParse<SyncFrequency>(settingsDto.SyncFrequency, true, out var syncFrequency))
            {
                return BadRequest($"Invalid sync frequency: {settingsDto.SyncFrequency}");
            }

            var settings = await _exportRepository.GetSettingsForUserAsync(userId);

            if (settings == null)
            {
                // create settings
                settings = new ExportSettings
                {
                    UserId = userId,
                    CalendarProvider = settingsDto.CalendarProvider,
                    ExternalCalendarId = settingsDto.ExternalCalendarId,
                    SyncFrequency = syncFrequency
                };

                settings = await _exportRepository.AddAsync(settings);
            }
            else
            {
                // update settings
                settings.CalendarProvider = settingsDto.CalendarProvider;
                settings.ExternalCalendarId = settingsDto.ExternalCalendarId;
                settings.SyncFrequency = syncFrequency;

                settings = await _exportRepository.UpdateAsync(settings);
            }

            return new ExportSettingsDto
            {
                Id = settings.Id,
                CalendarProvider = settings.CalendarProvider,
                ExternalCalendarId = settings.ExternalCalendarId,
                SyncFrequency = settings.SyncFrequency.ToString(),
                LastSyncDate = settings.LastSyncDate
            };
        }

        [HttpPost("synchronize")]
        public async Task<ActionResult<ExportResultDto>> SynchronizeCalendar()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var settings = await _exportRepository.GetSettingsForUserAsync(userId);

            if (settings == null)
                return BadRequest("Export settings have not been configured");

            var result = await _exportService.SynchronizeCalendarAsync(userId);

            if (!result.Success)
                return BadRequest(result.Message);

            // update last sync date
            settings.LastSyncDate = DateTime.UtcNow;
            await _exportRepository.UpdateAsync(settings);

            return new ExportResultDto
            {
                Success = true,
                EventsExported = result.EventsExported,
                Message = result.Message
            };
        }

        [HttpGet("preview")]
        public async Task<ActionResult<IEnumerable<CalendarEventDto>>> PreviewCalendarEvents(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var start = startDate ?? DateTime.UtcNow.Date;
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(30);

            var events = await _exportService.GetExportableEventsAsync(userId, start, end);

            return events.Select(e => new CalendarEventDto
            {
                Title = e.Title,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                IsTask = e.IsTask,
                IsFocusSession = e.IsFocusSession
            }).ToList();
        }

        [HttpGet("formats")]
        public ActionResult<IEnumerable<string>> GetSupportedFormats()
        {
            return new List<string>
            {
                "Google Calendar",
                "Microsoft Outlook",
                "Apple iCalendar",
                "ICS File"
            };
        }

        [HttpGet("providers")]
        public ActionResult<IEnumerable<CalendarProviderDto>> GetSupportedProviders()
        {
            return new List<CalendarProviderDto>
            {
                new CalendarProviderDto
                {
                    Id = "google",
                    Name = "Google Calendar",
                    SupportsAutoSync = true
                },
                new CalendarProviderDto
                {
                    Id = "outlook",
                    Name = "Microsoft Outlook",
                    SupportsAutoSync = true
                },
                new CalendarProviderDto
                {
                    Id = "apple",
                    Name = "Apple Calendar",
                    SupportsAutoSync = false
                },
                new CalendarProviderDto
                {
                    Id = "ics",
                    Name = "ICS File Download",
                    SupportsAutoSync = false
                }
            };
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadCalendar(
            [FromQuery] string format = "ics",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var start = startDate ?? DateTime.UtcNow.Date;
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(30);

            var result = await _exportService.GenerateCalendarFileAsync(userId, format, start, end);

            if (!result.Success)
                return BadRequest(result.Message);

            // file download
            return File(
                result.FileContent,
                result.ContentType,
                result.FileName
            );
        }

        [HttpDelete("settings")]
        public async Task<ActionResult> DeleteExportSettings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var settings = await _exportRepository.GetSettingsForUserAsync(userId);

            if (settings == null)
                return NoContent(); // already no settings?

            await _exportService.DisconnectCalendarAsync(userId);

            await _exportRepository.DeleteAsync(settings);

            return NoContent();
        }
    }
}