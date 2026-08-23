namespace EduMcq.Models;

// Models/Progress/UserProgressDto.cs
public class UserProgressDto
{
    public ProgressSummaryDto Summary { get; set; } = new();
    public AccuracyTrendDto AccuracyTrend { get; set; } = new();
    public ComparisonDto Comparison { get; set; } = new();
    public List<TopicPerformanceDto> TopicPerformance { get; set; } = new();
    public StreakDto Streak { get; set; } = new();
    public List<RecommendationDto> Recommendations { get; set; } = new();
}

public class ProgressSummaryDto
{
    public int QuizzesTaken { get; set; }
    public decimal AvgAccuracy { get; set; }
    public long TotalTimeSeconds { get; set; }
    public int TopicsMastered { get; set; }          // accuracy >= 80
    public int TopicsAttempted { get; set; }
}

public class AccuracyTrendDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
}

public class ComparisonDto
{
    public decimal YourScore { get; set; }
    public decimal ClassAverage { get; set; }
    public decimal TopPerformer { get; set; }
}

public class TopicPerformanceDto
{
    public Guid TopicId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int Attempted { get; set; }
    public decimal Accuracy { get; set; }
    public bool Weak { get; set; }                    // accuracy < 65
    public string Trend { get; set; } = "stable";     // up / down / stable (simple for now)
}

public class StreakDto
{
    public int CurrentStreak { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RecommendationDto
{
    public string Priority { get; set; } = "medium";  // high / medium / success
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? TopicId { get; set; }
}