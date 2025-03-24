using Microsoft.AspNetCore.SignalR;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using MonkMode.Infrastructure.RealTime;
using MonkMode.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonkMode.Domain.Enums;

namespace MonkMode.Infrastructure.Services
{
    public class FocusSessionService : IFocusSessionService
    {
        private readonly IFocusSessionRepository _focusSessionRepository;
        private readonly IHubContext<FocusSessionHub> _focusSessionHub;
        private readonly HubConnectionManager _connectionManager;

        public FocusSessionService(
            IFocusSessionRepository focusSessionRepository,
            IHubContext<FocusSessionHub> focusSessionHub,
            HubConnectionManager connectionManager)
        {
            _focusSessionRepository = focusSessionRepository;
            _focusSessionHub = focusSessionHub;
            _connectionManager = connectionManager;
        }

        public async Task<FocusSession> StartSessionAsync(int userId, FocusMode mode)
        {
            // check if there's already an active session
            var activeSession = await _focusSessionRepository.GetActiveSessionForUserAsync(userId);
            if (activeSession != null)
            {
                throw new InvalidOperationException("There is already an active focus session");
            }

            // create session
            var session = new FocusSession
            {
                UserId = userId,
                Mode = mode,
                StartTime = DateTime.UtcNow
            };

            var createdSession = await _focusSessionRepository.AddAsync(session);

            // notify connected clients
            await NotifySessionStartedAsync(userId, createdSession);

            return createdSession;
        }

        public async Task<FocusSession> EndSessionAsync(int sessionId, int userId)
        {
            var session = await _focusSessionRepository.FindByIdAsync(sessionId);

            if (session == null)
            {
                throw new ArgumentException($"Session with ID {sessionId} not found");
            }

            if (session.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only end your own sessions");
            }

            if (session.EndTime.HasValue)
            {
                throw new InvalidOperationException("Session is already ended");
            }

            session.EndTime = DateTime.UtcNow;

            // calc duration in minutes
            var duration = (int)Math.Round((session.EndTime.Value - session.StartTime).TotalMinutes);
            session.Duration = duration;

            var updatedSession = await _focusSessionRepository.UpdateAsync(session);

            // notify connected clients
            await NotifySessionEndedAsync(userId, updatedSession);

            return updatedSession;
        }

        public async Task<List<FocusSession>> GetSessionsForUserAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _focusSessionRepository.GetFocusSessionsForUserAsync(userId, startDate, endDate);
        }

        public async Task<FocusSession> GetActiveSessionAsync(int userId)
        {
            return await _focusSessionRepository.GetActiveSessionForUserAsync(userId);
        }

        public async Task<FocusSessionStats> GetSessionStatsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var sessions = await _focusSessionRepository.GetFocusSessionsForUserAsync(userId, startDate, endDate);

            // calc stats
            var stats = new FocusSessionStats
            {
                TotalSessions = sessions.Count,
                TotalMinutes = sessions.Sum(s => s.Duration ?? 0),
                ByMode = sessions
                    .GroupBy(s => s.Mode)
                    .Select(g => new FocusModeStats
                    {
                        Mode = g.Key,
                        SessionCount = g.Count(),
                        TotalMinutes = g.Sum(s => s.Duration ?? 0)
                    })
                    .ToList(),
                DailyStats = sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new DailyFocusStats
                    {
                        Date = g.Key,
                        TotalSessions = g.Count(),
                        TotalMinutes = g.Sum(s => s.Duration ?? 0)
                    })
                    .OrderBy(d => d.Date)
                    .ToList()
            };

            return stats;
        }

        private async Task NotifySessionStartedAsync(int userId, FocusSession session)
        {
            var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connectionIds.Any())
            {
                await _focusSessionHub.Clients.Clients(connectionIds).SendAsync(
                    "SessionStarted",
                    new FocusSessionDto
                    {
                        Id = session.Id,
                        FocusMode = session.Mode.ToString(),
                        StartTime = session.StartTime
                    });
            }
        }

        private async Task NotifySessionEndedAsync(int userId, FocusSession session)
        {
            var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connectionIds.Any())
            {
                await _focusSessionHub.Clients.Clients(connectionIds).SendAsync(
                    "SessionEnded",
                    new FocusSessionDto
                    {
                        Id = session.Id,
                        FocusMode = session.Mode.ToString(),
                        StartTime = session.StartTime,
                        EndTime = session.EndTime,
                        Duration = session.Duration
                    });
            }
        }
    }

    // support classes for stats
    public class FocusSessionStats
    {
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
        public List<FocusModeStats> ByMode { get; set; }
        public List<DailyFocusStats> DailyStats { get; set; }
    }

    public class FocusModeStats
    {
        public FocusMode Mode { get; set; }
        public int SessionCount { get; set; }
        public int TotalMinutes { get; set; }
    }

    public class DailyFocusStats
    {
        public DateTime Date { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
    }
}