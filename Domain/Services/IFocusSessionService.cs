using MonkMode.Domain.Enums;
using MonkMode.Domain.Models;
using MonkMode.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface IFocusSessionService
    {
        Task<FocusSession> StartSessionAsync(int userId, FocusMode mode);
        Task<FocusSession> EndSessionAsync(int sessionId, int userId);
        Task<List<FocusSession>> GetSessionsForUserAsync(int userId, DateTime startDate, DateTime endDate);
        Task<FocusSession> GetActiveSessionAsync(int userId);
        Task<FocusSessionStats> GetSessionStatsAsync(int userId, DateTime startDate, DateTime endDate);
    }
}