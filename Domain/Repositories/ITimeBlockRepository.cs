using MonkMode.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Domain.Repositories
{
    public interface ITimeBlockRepository : IRepository<TimeBlock>
    {
        Task<List<TimeBlock>> GetTimeBlocksForUserAsync(int userId, DateTime startDate, DateTime endDate);
        Task<TimeBlock> GetTimeBlockWithTasksAsync(int timeBlockId);
        Task<bool> HasOverlappingTimeBlocksAsync(int userId, DateTime startTime, DateTime endTime, int? excludeId = null);
    }
}