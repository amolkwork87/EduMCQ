using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class ProgressService
{
    private readonly IProgressRepository _repository;

    public ProgressService(IProgressRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserProgressDto> GetUserProgressAsync(Guid userId, int periodDays = 30)
    {
        var result = new UserProgressDto();

        result.Summary = await _repository.GetSummaryAsync(userId);
        result.TopicPerformance = await _repository.GetTopicPerformanceAsync(userId);

        result.Summary.TopicsMastered = result.TopicPerformance.Count(t => t.Accuracy >= 80);
        result.Summary.TopicsAttempted = result.TopicPerformance.Count;

        result.AccuracyTrend = await _repository.GetWeeklyTrendAsync(userId, periodDays);
        result.Comparison = await _repository.GetComparisonAsync(userId);
        result.Streak = await _repository.GetStreakAsync(userId);
        result.Recommendations = BuildRecommendations(result.TopicPerformance);

        return result;
    }

    private static List<RecommendationDto> BuildRecommendations(List<TopicPerformanceDto> topics)
    {
        var recs = new List<RecommendationDto>();

        var weak = topics.Where(t => t.Accuracy < 65).OrderBy(t => t.Accuracy).Take(2);
        foreach (var t in weak)
        {
            recs.Add(new RecommendationDto
            {
                Priority = t.Accuracy < 50 ? "high" : "medium",
                Title = $"Focus on {t.Topic}",
                Description = $"Your accuracy is {t.Accuracy:0}%. Practice more questions in this topic.",
                TopicId = t.TopicId
            });
        }

        var strong = topics.Where(t => t.Accuracy >= 80).OrderByDescending(t => t.Accuracy).Take(1);
        foreach (var t in strong)
        {
            recs.Add(new RecommendationDto
            {
                Priority = "success",
                Title = $"Keep momentum in {t.Topic}",
                Description = $"Excellent work! You're at {t.Accuracy:0}%. Maintain with light revisions.",
                TopicId = t.TopicId
            });
        }

        return recs;
    }
}