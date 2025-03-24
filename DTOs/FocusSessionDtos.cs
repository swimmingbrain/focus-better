using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MonkMode.DTOs
{
    public class FocusSessionDto
    {
        public int Id { get; set; }
        public string FocusMode { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? Duration { get; set; } // Duration in minutes
    }

    public class StartFocusSessionDto
    {
        [Required]
        public string FocusMode { get; set; }
    }

    public class FocusSessionStatsDto
    {
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
        public List<FocusModeStatsDto> ByMode { get; set; }
        public List<DailyFocusStatsDto> DailyStats { get; set; }
    }

    public class FocusModeStatsDto
    {
        public string Mode { get; set; }
        public int SessionCount { get; set; }
        public int TotalMinutes { get; set; }
    }

    public class DailyFocusStatsDto
    {
        public DateTime Date { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
    }
}