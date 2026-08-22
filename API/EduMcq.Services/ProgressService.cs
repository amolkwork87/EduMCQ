using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduMcq.Services;

// Services/ProgressService.cs
using Npgsql;
using System.Data;

public class ProgressService
{
    private readonly string _connectionString;

    public ProgressService(IConfiguration config)
    {
        _connectionString = config["EduMcqDatabaseConnection"]!;
    }

    public async Task<UserProgressDto> GetUserProgressAsync(Guid userId, int periodDays = 30)
    {
        var result = new UserProgressDto();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Summary
        result.Summary = await GetSummaryAsync(conn, userId);

        // 2. Topic performance
        result.TopicPerformance = await GetTopicPerformanceAsync(conn, userId);

        // 3. Topics mastered
        result.Summary.TopicsMastered = result.TopicPerformance.Count(t => t.Accuracy >= 80);
        result.Summary.TopicsAttempted = result.TopicPerformance.Count;

        // 4. Weekly accuracy trend
        result.AccuracyTrend = await GetWeeklyTrendAsync(conn, userId, periodDays);

        // 5. Comparison (your score vs class)
        result.Comparison = await GetComparisonAsync(conn, userId);

        // 6. Simple streak (consecutive days with at least one attempt)
        result.Streak = await GetStreakAsync(conn, userId);

        // 7. Recommendations from weak topics
        result.Recommendations = BuildRecommendations(result.TopicPerformance);

        return result;
    }

    // ------------------------------------------------------------------
    private async Task<ProgressSummaryDto> GetSummaryAsync(NpgsqlConnection conn, Guid userId)
    {
        const string sql = @"
            SELECT 
                quizzes_taken,
                avg_accuracy,
                total_time_seconds
            FROM mv_user_progress
            WHERE user_id = @userId";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProgressSummaryDto
            {
                QuizzesTaken = reader.GetInt32(0),
                AvgAccuracy = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                TotalTimeSeconds = reader.IsDBNull(2) ? 0 : reader.GetInt64(2)
            };
        }

        return new ProgressSummaryDto(); // no data yet
    }

    // ------------------------------------------------------------------
    private async Task<List<TopicPerformanceDto>> GetTopicPerformanceAsync(NpgsqlConnection conn, Guid userId)
    {
        const string sql = @"
            SELECT 
                topic_id,
                topic_name,
                questions_attempted,
                accuracy
            FROM mv_user_topic_performance
            WHERE user_id = @userId
            ORDER BY accuracy ASC";   // weakest first

        var list = new List<TopicPerformanceDto>();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var accuracy = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3);
            list.Add(new TopicPerformanceDto
            {
                TopicId = reader.GetGuid(0),
                Topic = reader.GetString(1),
                Attempted = reader.GetInt32(2),
                Accuracy = accuracy,
                Weak = accuracy < 65,
                Trend = "stable"   // can be improved later
            });
        }

        return list;
    }

    // ------------------------------------------------------------------
    private async Task<AccuracyTrendDto> GetWeeklyTrendAsync(NpgsqlConnection conn, Guid userId, int periodDays)
    {
        const string sql = @"
            SELECT 
                week_start,
                avg_accuracy
            FROM mv_user_weekly_accuracy
            WHERE user_id = @userId
              AND week_start >= CURRENT_DATE - (@periodDays || ' days')::interval
            ORDER BY week_start";

        var labels = new List<string>();
        var data = new List<decimal>();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("periodDays", periodDays);

        await using var reader = await cmd.ExecuteReaderAsync();
        int weekNum = 1;
        while (await reader.ReadAsync())
        {
            labels.Add($"Week {weekNum++}");
            data.Add(reader.IsDBNull(1) ? 0 : reader.GetDecimal(1));
        }

        return new AccuracyTrendDto { Labels = labels, Data = data };
    }

    // ------------------------------------------------------------------
    private async Task<ComparisonDto> GetComparisonAsync(NpgsqlConnection conn, Guid userId)
    {
        // Your score
        const string mySql = @"
            SELECT avg_accuracy FROM mv_user_progress WHERE user_id = @userId";

        decimal yourScore = 0;
        await using (var cmd = new NpgsqlCommand(mySql, conn))
        {
            cmd.Parameters.AddWithValue("userId", userId);
            var val = await cmd.ExecuteScalarAsync();
            if (val != null && val != DBNull.Value)
                yourScore = Convert.ToDecimal(val);
        }

        // Class average + top performer
        const string classSql = @"
            SELECT 
                ROUND(AVG(avg_accuracy)::numeric, 2) AS class_avg,
                ROUND(MAX(avg_accuracy)::numeric, 2) AS top_score
            FROM mv_user_progress
            WHERE quizzes_taken > 0";

        decimal classAvg = 0, topScore = 0;
        await using (var cmd = new NpgsqlCommand(classSql, conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                classAvg = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                topScore = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
            }
        }

        return new ComparisonDto
        {
            YourScore = yourScore,
            ClassAverage = classAvg,
            TopPerformer = topScore
        };
    }

    // ------------------------------------------------------------------
    private async Task<StreakDto> GetStreakAsync(NpgsqlConnection conn, Guid userId)
    {
        // Simple version: count consecutive days ending today/yesterday
        const string sql = @"
            WITH days AS (
                SELECT DISTINCT completed_at::date AS d
                FROM test_attempts
                WHERE user_id = @userId
                ORDER BY d DESC
            ),
            numbered AS (
                SELECT d, ROW_NUMBER() OVER (ORDER BY d DESC) AS rn
                FROM days
            )
            SELECT COUNT(*) 
            FROM numbered
            WHERE d = CURRENT_DATE - (rn - 1) * INTERVAL '1 day'";

        int streak = 0;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        var val = await cmd.ExecuteScalarAsync();
        if (val != null && val != DBNull.Value)
            streak = Convert.ToInt32(val);

        return new StreakDto
        {
            CurrentStreak = streak,
            Message = streak > 0
                ? $"You're on a {streak}-day learning streak. Keep it going!"
                : "Start a quiz today to begin your streak!"
        };
    }

    // ------------------------------------------------------------------
    private List<RecommendationDto> BuildRecommendations(List<TopicPerformanceDto> topics)
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