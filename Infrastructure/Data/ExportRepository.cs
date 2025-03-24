using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Data
{
    public class ExportRepository : IExportRepository
    {
        private readonly AppDbContext _context;

        public ExportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExportSettings> AddAsync(ExportSettings entity)
        {
            await _context.ExportSettings.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(ExportSettings entity)
        {
            _context.ExportSettings.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<CalendarEvent> AddEventAsync(CalendarEvent calendarEvent)
        {
            await _context.CalendarEvents.AddAsync(calendarEvent);
            await _context.SaveChangesAsync();
            return calendarEvent;
        }

        public async Task DeleteAllEventsAsync(int settingsId)
        {
            var events = await _context.CalendarEvents
                .Where(e => e.ExportSettingsId == settingsId)
                .ToListAsync();

            _context.CalendarEvents.RemoveRange(events);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ExportSettings>> FindAllAsync()
        {
            return await _context.ExportSettings.ToListAsync();
        }

        public async Task<ExportSettings> FindByIdAsync(int id)
        {
            return await _context.ExportSettings.FindAsync(id);
        }

        public async Task<CalendarEvent> GetEventByExternalIdAsync(int userId, string externalEventId)
        {
            var settings = await GetSettingsForUserAsync(userId);

            if (settings == null)
                return null;

            return await _context.CalendarEvents
                .FirstOrDefaultAsync(e => e.ExportSettingsId == settings.Id &&
                                        e.ExternalEventId == externalEventId);
        }

        public async Task<List<CalendarEvent>> GetEventsForUserAsync(int userId)
        {
            var settings = await GetSettingsForUserAsync(userId);

            if (settings == null)
                return new List<CalendarEvent>();

            return await _context.CalendarEvents
                .Where(e => e.ExportSettingsId == settings.Id)
                .ToListAsync();
        }

        public async Task<ExportSettings> GetSettingsForUserAsync(int userId)
        {
            return await _context.ExportSettings
                .FirstOrDefaultAsync(e => e.UserId == userId);
        }

        public async Task<ExportSettings> UpdateAsync(ExportSettings entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }
    }
}