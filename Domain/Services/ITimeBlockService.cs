using MonkMode.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Services
{
    public interface ITimeBlockService
    {
        Task<ServiceResult<TimeBlock>> CreateTimeBlockAsync(TimeBlock timeBlock);
        Task<ServiceResult<TimeBlock>> UpdateTimeBlockAsync(TimeBlock timeBlock);
        Task<ServiceResult<bool>> CheckForOverlappingTimeBlocksAsync(int userId, DateTime startTime, DateTime endTime, int? excludeId = null);
        Task<List<TimeBlock>> GetTimeBlocksForDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    }
}