using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EduMcq.Repository;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly string _connectionString;
    private static readonly Dictionary<string, int> PlanQuestionLimits = new()
    {
        ["free"] = 100,
        ["pro"] = -1,
        ["elite"] = -1
    };

    public SubscriptionRepository(IConfiguration configuration)
    {
        _connectionString = configuration["EduMcqDatabaseConnection"]
            ?? throw new InvalidOperationException("EduMcqDatabaseConnection is not configured.");
    }

    public async Task<SubscriptionDto> GetActiveSubscriptionAsync(Guid userId)
    {
        const string sql = @"
            SELECT id, plan, status, amount_paise, currency,
                   started_at, expires_at, cancelled_at, auto_renew
            FROM subscriptions
            WHERE user_id = @userId AND status = 'active'
            ORDER BY started_at DESC
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SubscriptionDto
            {
                Id = reader.GetGuid(0),
                Plan = reader.GetString(1),
                Status = reader.GetString(2),
                AmountPaise = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Currency = reader.IsDBNull(4) ? "INR" : reader.GetString(4),
                StartedAt = reader.GetDateTime(5),
                ExpiresAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CancelledAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                AutoRenew = reader.GetBoolean(8)
            };
        }

        return new SubscriptionDto
        {
            Plan = "free",
            Status = "active",
            StartedAt = DateTime.UtcNow
        };
    }

    public async Task<UsageDto> GetMonthlyUsageAsync(Guid userId, string plan)
    {
        const string sql = @"
            SELECT 
                COALESCE(SUM(total_questions), 0) AS questions_attempted,
                COUNT(*) AS quizzes_taken
            FROM test_attempts
            WHERE user_id = @userId
              AND completed_at >= date_trunc('month', CURRENT_DATE)
              AND completed_at IS NOT NULL";

        int questionsUsed = 0;
        int quizzesTaken = 0;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            questionsUsed = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetInt64(0));
            quizzesTaken = reader.GetInt32(1);
        }

        var allowed = PlanQuestionLimits.GetValueOrDefault(plan, 100);

        return new UsageDto
        {
            QuestionsUsed = questionsUsed,
            QuestionsAllowed = allowed,
            QuizzesTakenThisMonth = quizzesTaken
        };
    }

    public async Task<List<BillingHistoryItemDto>> GetBillingHistoryAsync(Guid userId)
    {
        const string sql = @"
            SELECT id, started_at, plan, amount_paise, status
            FROM subscriptions
            WHERE user_id = @userId
              AND plan <> 'free'
            ORDER BY started_at DESC
            LIMIT 20";

        var list = new List<BillingHistoryItemDto>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var amountPaise = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            list.Add(new BillingHistoryItemDto
            {
                Id = reader.GetGuid(0),
                Date = reader.GetDateTime(1),
                Plan = reader.GetString(2),
                Amount = $"₹{amountPaise}",
                Status = reader.GetString(4) == "active" || reader.GetString(4) == "expired"
                    ? "Paid"
                    : reader.GetString(4)
            });
        }

        return list;
    }

    public async Task<(bool Success, string? Error, SubscriptionDto? Data)> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise)
    {
        if (plan is not ("pro" or "elite"))
            return (false, "Invalid plan", null);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string cancelSql = @"
            UPDATE subscriptions
            SET status = 'cancelled', cancelled_at = now(), updated_at = now()
            WHERE user_id = @userId AND status = 'active'";

        await using (var cancelCommand = new NpgsqlCommand(cancelSql, connection))
        {
            cancelCommand.Parameters.AddWithValue("userId", userId);
            await cancelCommand.ExecuteNonQueryAsync();
        }

        const string insertSql = @"
            INSERT INTO subscriptions (
                user_id, plan, status, amount_paise, currency,
                started_at, expires_at, auto_renew
            ) VALUES (
                @userId, @plan, 'active', @amount, 'INR',
                now(), now() + interval '1 year', true
            )
            RETURNING id, plan, status, amount_paise, currency,
                      started_at, expires_at, cancelled_at, auto_renew";

        await using var insertCommand = new NpgsqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("userId", userId);
        insertCommand.Parameters.AddWithValue("plan", plan);
        insertCommand.Parameters.AddWithValue("amount", amountPaise);

        await using var reader = await insertCommand.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var dto = new SubscriptionDto
            {
                Id = reader.GetGuid(0),
                Plan = reader.GetString(1),
                Status = reader.GetString(2),
                AmountPaise = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Currency = reader.GetString(4),
                StartedAt = reader.GetDateTime(5),
                ExpiresAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                CancelledAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                AutoRenew = reader.GetBoolean(8)
            };
            return (true, null, dto);
        }

        return (false, "Failed to create subscription", null);
    }

    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid userId)
    {
        const string sql = @"
            UPDATE subscriptions
            SET status = 'cancelled',
                cancelled_at = now(),
                auto_renew = false,
                updated_at = now()
            WHERE user_id = @userId AND status = 'active'";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0 ? (true, null) : (false, "No active subscription found");
    }

    public async Task<(bool Allowed, string? Message)> CanUserTakeTestAsync(Guid userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);

        if (subscription.Plan != "free" && subscription.ExpiresAt.HasValue && subscription.ExpiresAt < DateTime.UtcNow)
            subscription.Plan = "free";

        if (subscription.Plan == "free")
        {
            const string usageSql = @"
                SELECT COALESCE(SUM(total_questions), 0)
                FROM test_attempts
                WHERE user_id = @userId
                  AND completed_at >= date_trunc('month', CURRENT_DATE)";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(usageSql, connection);
            command.Parameters.AddWithValue("userId", userId);

            var used = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);

            if (used >= 100)
                return (false, "You have reached the free limit of 100 questions this month. Please upgrade to continue.");
        }

        return (true, null);
    }
}
