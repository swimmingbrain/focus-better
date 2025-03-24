using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.DTOs;
using MonkMode.Infrastructure.RealTime;
using MonkMode.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace MonkMode.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FocusSessionsController : ControllerBase
    {
        private readonly IFocusSessionRepository _focusSessionRepository;
        private readonly IHubContext<FocusSessionHub> _focusSessionHub;
        private readonly HubConnectionManager _connectionManager;

        public FocusSessionsController(
            IFocusSessionRepository focusSessionRepository,
            IHubContext<FocusSessionHub> focusSessionHub,
            HubConnectionManager connectionManager)
        {
            _focusSessionRepository = focusSessionRepository;
            _focusSessionHub = focusSessionHub;
            _connectionManager = connectionManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FocusSessionDto>>> GetSessions(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var sessions = await _focusSessionRepository.GetFocusSessionsForUserAsync(
                userId,
                startDate ?? DateTime.UtcNow.Date.AddDays(-30),
                endDate ?? DateTime.UtcNow.Date.AddDays(1)
            );

            var sessionDtos = sessions.Select(s => new FocusSessionDto
            {
                Id = s.Id,
                FocusMode = s.Mode.ToString(),
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Duration = s.Duration
            }).ToList();

            return sessionDtos;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<FocusSessionStatsDto>> GetSessionStats(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            var sessions = await _focusSessionRepository.GetFocusSessionsForUserAsync(userId, start, end);

            var stats = new FocusSessionStatsDto
            {
                TotalSessions = sessions.Count,
                TotalMinutes = sessions.Sum(s => s.Duration ?? 0),
                ByMode = sessions
                    .GroupBy(s => s.Mode)
                    .Select(g => new FocusModeStatsDto
                    {
                        Mode = g.Key.ToString(),
                        SessionCount = g.Count(),
                        TotalMinutes = g.Sum(s => s.Duration ?? 0)
                    })
                    .ToList(),
                DailyStats = sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new DailyFocusStatsDto
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

        [HttpGet("{id}")]
        public async Task<ActionResult<FocusSessionDto>> GetSession(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var session = await _focusSessionRepository.FindByIdAsync(id);

            if (session == null)
                return NotFound();

            // make sure session gehört zu user
            if (session.UserId != userId)
                return Forbid();

            return new FocusSessionDto
            {
                Id = session.Id,
                FocusMode = session.Mode.ToString(),
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration
            };
        }

        [HttpPost("start")]
        public async Task<ActionResult<FocusSessionDto>> StartSession(StartFocusSessionDto startSessionDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // parse enum
            if (!Enum.TryParse<FocusMode>(startSessionDto.FocusMode, true, out var focusMode))
            {
                return BadRequest($"Invalid focus mode: {startSessionDto.FocusMode}");
            }

            // check active session
            var activeSession = await _focusSessionRepository.GetActiveSessionForUserAsync(userId);
            if (activeSession != null)
            {
                return BadRequest("There is already an active focus session");
            }

            var session = new FocusSession
            {
                UserId = userId,
                Mode = focusMode,
                StartTime = DateTime.UtcNow
            };

            var createdSession = await _focusSessionRepository.AddAsync(session);

            // notify via signalR
            var connectionIds = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connectionIds.Any())
            {
                await _focusSessionHub.Clients.Clients(connectionIds).SendAsync(
                    "SessionStarted",
                    new FocusSessionDto
                    {
                        Id = createdSession.Id,
                        FocusMode = createdSession.Mode.ToString(),
                        StartTime = createdSession.StartTime
                    });
            }

            return CreatedAtAction(nameof(GetSession), new { id = createdSession.Id }, new FocusSessionDto
            {
                Id = createdSession.Id,
                FocusMode = createdSession.Mode.ToString(),
                StartTime = createdSession.StartTime
            });
        }

        [HttpPost("{id}/end")]
        public async Task<ActionResult<FocusSessionDto>> EndSession(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var session = await _focusSessionRepository.FindByIdAsync(id);

            if (session == null)
                return NotFound();

            // make sure session gehört zu user
            if (session.UserId != userId)
                return Forbid();

            // session ended?
            if (session.EndTime.HasValue)
            {
                return BadRequest("Session is already ended");
            }

            // end
            session.EndTime = DateTime.UtcNow;

            // calc duration in minutes
            var duration = (int)Math.Round((session.EndTime.Value - session.StartTime).TotalMinutes);
            session.Duration = duration;

            await _focusSessionRepository.UpdateAsync(session);

            // notify via signalR
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

            return new FocusSessionDto
            {
                Id = session.Id,
                FocusMode = session.Mode.ToString(),
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration
            };
        }

        [HttpGet("active")]
        public async Task<ActionResult<FocusSessionDto>> GetActiveSession()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var session = await _focusSessionRepository.GetActiveSessionForUserAsync(userId);

            if (session == null)
                return NoContent(); // No active session

            return new FocusSessionDto
            {
                Id = session.Id,
                FocusMode = session.Mode.ToString(),
                StartTime = session.StartTime
            };
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSession(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var session = await _focusSessionRepository.FindByIdAsync(id);

            if (session == null)
                return NotFound();

            // make sure session gehört zu user
            if (session.UserId != userId)
                return Forbid();

            await _focusSessionRepository.DeleteAsync(session);
            return NoContent();
        }
    }
}