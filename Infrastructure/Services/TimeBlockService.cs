using MonkMode.Domain.Models;
using MonkMode.Domain.Repositories;
using MonkMode.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonkMode.Infrastructure.Services
{
    public class TimeBlockService : ITimeBlockService
    {
        private readonly ITimeBlockRepository _timeBlockRepository;

        public TimeBlockService(ITimeBlockRepository timeBlockRepository)
        {
            _timeBlockRepository = timeBlockRepository;
        }

        public async Task<ServiceResult<TimeBlock>> CreateTimeBlockAsync(TimeBlock timeBlock)
        {
            try
            {
                // validate time range
                if (timeBlock.EndTime <= timeBlock.StartTime)
                {
                    return ServiceResult<TimeBlock>.CreateError("End time must be after start time");
                }

                // check for overlapping time blocks
                var hasOverlap = await CheckForOverlappingTimeBlocksAsync(
                    timeBlock.UserId,
                    timeBlock.StartTime,
                    timeBlock.EndTime);

                if (hasOverlap.Data)
                {
                    var createdTimeBlock = await _timeBlockRepository.AddAsync(timeBlock);
                    return ServiceResult<TimeBlock>.CreateSuccess(
                        createdTimeBlock,
                        "Time block created successfully, but it overlaps with existing time blocks.");
                }

                var newTimeBlock = await _timeBlockRepository.AddAsync(timeBlock);
                return ServiceResult<TimeBlock>.CreateSuccess(newTimeBlock);
            }
            catch (Exception ex)
            {
                return ServiceResult<TimeBlock>.CreateError($"Failed to create time block: {ex.Message}");
            }
        }

        public async Task<ServiceResult<TimeBlock>> UpdateTimeBlockAsync(TimeBlock timeBlock)
        {
            try
            {
                // validate time range
                if (timeBlock.EndTime <= timeBlock.StartTime)
                {
                    return ServiceResult<TimeBlock>.CreateError("End time must be after start time");
                }

                // check for overlapping time blocks
                var hasOverlap = await CheckForOverlappingTimeBlocksAsync(
                    timeBlock.UserId,
                    timeBlock.StartTime,
                    timeBlock.EndTime,
                    timeBlock.Id);

                if (hasOverlap.Data)
                {
                    // update timeblock but with warning
                    var updatedTimeBlock = await _timeBlockRepository.UpdateAsync(timeBlock);
                    return ServiceResult<TimeBlock>.CreateSuccess(
                        updatedTimeBlock,
                        "Time block updated successfully, but it overlaps with existing time blocks.");
                }

                var newTimeBlock = await _timeBlockRepository.UpdateAsync(timeBlock);
                return ServiceResult<TimeBlock>.CreateSuccess(newTimeBlock);
            }
            catch (Exception ex)
            {
                return ServiceResult<TimeBlock>.CreateError($"Failed to update time block: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CheckForOverlappingTimeBlocksAsync(
            int userId,
            DateTime startTime,
            DateTime endTime,
            int? excludeId = null)
        {
            try
            {
                var hasOverlap = await _timeBlockRepository.HasOverlappingTimeBlocksAsync(
                    userId,
                    startTime,
                    endTime,
                    excludeId);

                return ServiceResult<bool>.CreateSuccess(hasOverlap);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.CreateError($"Failed to check for overlapping time blocks: {ex.Message}");
            }
        }

        public async Task<List<TimeBlock>> GetTimeBlocksForDateRangeAsync(
            int userId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _timeBlockRepository.GetTimeBlocksForUserAsync(userId, startDate, endDate);
        }
    }
}