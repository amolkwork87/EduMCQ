using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface IProgressRepository
{
    Task<ProgressSummaryDto> GetSummaryAsync(Guid userId);
    Task<List<TopicPerformanceDto>> GetTopicPerformanceAsync(Guid userId);
    Task<AccuracyTrendDto> GetWeeklyTrendAsync(Guid userId, int periodDays);
    Task<ComparisonDto> GetComparisonAsync(Guid userId);
    Task<StreakDto> GetStreakAsync(Guid userId);
}
