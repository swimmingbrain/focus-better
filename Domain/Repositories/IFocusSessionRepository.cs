using MonkMode.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface IFocusSessionRepository : IRepository<FocusSession>
    {
        Task<List<FocusSession>> GetFocusSessionsForUserAsync(int userId, DateTime startDate, DateTime endDate);
        Task<FocusSession> GetActiveSessionForUserAsync(int userId);
    }
}