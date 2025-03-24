using MonkMode.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface IExportRepository : IRepository<ExportSettings>
    {
        Task<ExportSettings> GetSettingsForUserAsync(int userId);
        Task<List<CalendarEvent>> GetEventsForUserAsync(int userId);
        Task<CalendarEvent> GetEventByExternalIdAsync(int userId, string externalEventId);
        Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent);
        Task DeleteAllEventsAsync(int settingsId);
    }
}