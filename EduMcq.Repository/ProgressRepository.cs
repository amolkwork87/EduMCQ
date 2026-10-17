using EduMcq.Interfaces;
using EduMcq.Models;
using MySqlConnector;

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
            SELECT COUNT(*) AS quizzes_taken,
                   ROUND(AVG(score_percentage), 2) AS avg_accuracy,
                   COALESCE(SUM(time_taken_seconds), 0) AS total_time_seconds
            FROM test_attempts
            WHERE user_id = @userId";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
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
            SELECT q.topic_id, t.name AS topic_name,
                   COUNT(*) AS questions_attempted,
                   ROUND(100.0 * SUM(CASE WHEN ta_answer.is_correct THEN 1 ELSE 0 END) / COUNT(*), 2) AS accuracy
            FROM test_attempts ta
            JOIN test_answers ta_answer ON ta_answer.attempt_id = ta.id
            JOIN questions q ON q.id = ta_answer.question_id
            JOIN topics t ON t.id = q.topic_id
            WHERE ta.user_id = @userId AND q.topic_id IS NOT NULL
            GROUP BY q.topic_id, t.name
            ORDER BY accuracy ASC";

        var list = new List<TopicPerformanceDto>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
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
                        SELECT DATE_SUB(DATE(completed_at), INTERVAL WEEKDAY(completed_at) DAY) AS week_start,
                                     ROUND(AVG(score_percentage), 2) AS avg_accuracy
                        FROM test_attempts
                        WHERE user_id = @userId
                            AND completed_at >= DATE_SUB(CURRENT_DATE, INTERVAL @periodDays DAY)
                        GROUP BY week_start
                        ORDER BY week_start";

        var labels = new List<string>();
        var data = new List<decimal>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
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
            SELECT ROUND(AVG(score_percentage), 2) FROM test_attempts WHERE user_id = @userId";

        decimal yourScore = 0;
        await using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var command = new MySqlCommand(mySql, connection);
            command.Parameters.AddWithValue("userId", userId);
            var value = await command.ExecuteScalarAsync();
            if (value != null && value != DBNull.Value)
                yourScore = Convert.ToDecimal(value);
        }

        const string classSql = @"
            SELECT 
                ROUND(AVG(user_accuracy), 2) AS class_avg,
                ROUND(MAX(user_accuracy), 2) AS top_score
            FROM (
                SELECT user_id, AVG(score_percentage) AS user_accuracy
                FROM test_attempts
                GROUP BY user_id
            ) user_scores";

        decimal classAvg = 0;
        decimal topScore = 0;

        await using (var connection = new MySqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var command = new MySqlCommand(classSql, connection);
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
                SELECT DISTINCT DATE(completed_at) AS d
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
            WHERE d = DATE_SUB(CURRENT_DATE, INTERVAL (rn - 1) DAY)";

        int streak = 0;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
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
