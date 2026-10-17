// Repositories/LeaderboardRepository.cs
using System.Data;
using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace EduMcq.Repository;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly string _connectionString;

    public LeaderboardRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetGlobalAsync(
        Guid currentUserId, string exam, int limit)
    {
        const string sql = @"
            WITH user_stats AS (
                SELECT user_id, COUNT(*) AS quizzes, COALESCE(SUM(correct_count), 0) AS score,
                       ROUND(AVG(score_percentage), 1) AS accuracy
                FROM test_attempts GROUP BY user_id
            ), ranked AS (
                SELECT user_stats.*, RANK() OVER (ORDER BY score DESC, accuracy DESC, quizzes DESC) AS `rank`
                FROM user_stats
            )
            SELECT ranked.`rank`, COALESCE(users.name, users.email, 'Anonymous') AS name,
                   ranked.score, ranked.quizzes, ranked.accuracy,
                   (ranked.user_id = @currentUserId) AS is_current_user
            FROM ranked JOIN users ON users.id = ranked.user_id
            ORDER BY ranked.`rank` LIMIT @limit";

        return await QueryLeaderboardAsync(sql, currentUserId, exam, limit);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyAsync(
        Guid currentUserId, string exam, int limit)
    {
        const string sql = @"
            WITH user_stats AS (
                SELECT user_id, COUNT(*) AS quizzes, COALESCE(SUM(correct_count), 0) AS score,
                       ROUND(AVG(score_percentage), 1) AS accuracy
                FROM test_attempts
                WHERE completed_at >= DATE_SUB(CURRENT_TIMESTAMP, INTERVAL WEEKDAY(CURRENT_TIMESTAMP) DAY)
                GROUP BY user_id
            ), ranked AS (
                SELECT user_stats.*, RANK() OVER (ORDER BY score DESC, accuracy DESC, quizzes DESC) AS `rank`
                FROM user_stats
            )
            SELECT ranked.`rank`, COALESCE(users.name, users.email, 'Anonymous') AS name,
                   ranked.score, ranked.quizzes, ranked.accuracy,
                   (ranked.user_id = @currentUserId) AS is_current_user
            FROM ranked JOIN users ON users.id = ranked.user_id
            ORDER BY ranked.`rank` LIMIT @limit";

        return await QueryLeaderboardAsync(sql, currentUserId, exam, limit);
    }

    public async Task<UserLeaderboardStatsDto> GetMyStatsAsync(Guid currentUserId)
    {
        const string sql = @"
            WITH user_stats AS (
                SELECT user_id, COUNT(*) AS quizzes, COALESCE(SUM(correct_count), 0) AS score,
                       ROUND(AVG(score_percentage), 1) AS accuracy
                FROM test_attempts GROUP BY user_id
            ), ranked AS (
                SELECT user_stats.*, RANK() OVER (ORDER BY score DESC, accuracy DESC, quizzes DESC) AS `rank`
                FROM user_stats
            )
            SELECT `rank` AS global_rank, score AS total_points, accuracy, quizzes AS quizzes_taken
            FROM ranked WHERE user_id = @currentUserId";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("currentUserId", currentUserId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new UserLeaderboardStatsDto
            {
                GlobalRank   = reader.IsDBNull("global_rank") ? null : reader.GetInt32("global_rank"),
                TotalPoints  = reader.GetInt32("total_points"),
                Accuracy     = reader.GetDecimal("accuracy"),
                QuizzesTaken = reader.GetInt32("quizzes_taken")
            };
        }

        // User has never taken a test
        return new UserLeaderboardStatsDto
        {
            GlobalRank   = null,
            TotalPoints  = 0,
            Accuracy     = 0,
            QuizzesTaken = 0
        };
    }

    // ---------- private helper ----------
    private async Task<IReadOnlyList<LeaderboardEntryDto>> QueryLeaderboardAsync(
        string sql, Guid currentUserId, string exam, int limit)
    {
        var list = new List<LeaderboardEntryDto>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("currentUserId", currentUserId);
        cmd.Parameters.AddWithValue("exam", exam ?? "all");
        cmd.Parameters.AddWithValue("limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new LeaderboardEntryDto
            {
                Rank          = reader.GetInt32("rank"),
                Name          = reader.GetString("name"),
                Score         = reader.GetInt32("score"),
                Quizzes       = reader.GetInt32("quizzes"),
                Accuracy      = reader.GetDecimal("accuracy"),
                IsCurrentUser = reader.GetBoolean("is_current_user")
            });
        }

        return list;
    }
}