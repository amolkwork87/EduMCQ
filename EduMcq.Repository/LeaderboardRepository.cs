// Repositories/LeaderboardRepository.cs
using System.Data;
using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

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
            SELECT * FROM fn_get_leaderboard_global(@currentUserId, @exam, @limit);";

        return await QueryLeaderboardAsync(sql, currentUserId, exam, limit);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyAsync(
        Guid currentUserId, string exam, int limit)
    {
        const string sql = @"
            SELECT * FROM fn_get_leaderboard_weekly(@currentUserId, @exam, @limit);";

        return await QueryLeaderboardAsync(sql, currentUserId, exam, limit);
    }

    public async Task<UserLeaderboardStatsDto> GetMyStatsAsync(Guid currentUserId)
    {
        const string sql = @"
            SELECT * FROM fn_get_my_leaderboard_stats(@currentUserId);";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
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

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
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