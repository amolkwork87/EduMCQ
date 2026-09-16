using EduMcq.Interfaces;
using EduMcq.Models;
using Npgsql;

namespace EduMcq.Repository;

public class ProgressRepository : IProgressRepository
{
    private readonly string _connectionString;

    public ProgressRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<ProgressSummaryDto> GetSummaryAsync(Guid userId)
    {
        const string sql = @"
            SELECT 
                quizzes_taken,
                avg_accuracy,
                total_time_seconds
            FROM mv_user_progress
            WHERE user_id = @userId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProgressSummaryDto
            {
                QuizzesTaken = reader.GetInt32(0),
                AvgAccuracy = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1),
                TotalTimeSeconds = reader.IsDBNull(2) ? 0 : reader.GetInt64(2)
            };
        }

        return new ProgressSummaryDto();
    }

    public async Task<List<TopicPerformanceDto>> GetTopicPerformanceAsync(Guid userId)
    {
        const string sql = @"
            SELECT 
                topic_id,
                topic_name,
                questions_attempted,
                accuracy
            FROM mv_user_topic_performance
            WHERE user_id = @userId
            ORDER BY accuracy ASC";

        var list = new List<TopicPerformanceDto>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
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
                Trend = "stable"
            });
        }

        return list;
    }

    public async Task<AccuracyTrendDto> GetWeeklyTrendAsync(Guid userId, int periodDays)
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

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("periodDays", periodDays);

        await using var reader = await command.ExecuteReaderAsync();
        int weekNum = 1;
        while (await reader.ReadAsync())
        {
            labels.Add($"Week {weekNum++}");
            data.Add(reader.IsDBNull(1) ? 0 : reader.GetDecimal(1));
        }

        return new AccuracyTrendDto { Labels = labels, Data = data };
    }

    public async Task<ComparisonDto> GetComparisonAsync(Guid userId)
    {
        const string mySql = @"
            SELECT avg_accuracy FROM mv_user_progress WHERE user_id = @userId";

        decimal yourScore = 0;
        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(mySql, connection);
            command.Parameters.AddWithValue("userId", userId);
            var value = await command.ExecuteScalarAsync();
            if (value != null && value != DBNull.Value)
                yourScore = Convert.ToDecimal(value);
        }

        const string classSql = @"
            SELECT 
                ROUND(AVG(avg_accuracy)::numeric, 2) AS class_avg,
                ROUND(MAX(avg_accuracy)::numeric, 2) AS top_score
            FROM mv_user_progress
            WHERE quizzes_taken > 0";

        decimal classAvg = 0;
        decimal topScore = 0;

        await using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(classSql, connection);
            await using var reader = await command.ExecuteReaderAsync();
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

    public async Task<StreakDto> GetStreakAsync(Guid userId)
    {
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
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        var value = await command.ExecuteScalarAsync();
        if (value != null && value != DBNull.Value)
            streak = Convert.ToInt32(value);

        return new StreakDto
        {
            CurrentStreak = streak,
            Message = streak > 0
                ? $"You're on a {streak}-day learning streak. Keep it going!"
                : "Start a quiz today to begin your streak!"
        };
    }
}
