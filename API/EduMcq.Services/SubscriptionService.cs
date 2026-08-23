using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

public class SubscriptionService
{
    private readonly string _cs;

    // Plan limits
    private static readonly Dictionary<string, int> PlanQuestionLimits = new()
    {
        ["free"]  = 100,
        ["pro"]   = -1,   // unlimited
        ["elite"] = -1
    };

    public SubscriptionService(IConfiguration config)
    {
        _cs = config["EduMcqDatabaseConnection"]!;
    }

    // ------------------------------------------------------------------
    // Main method used by the page
    // ------------------------------------------------------------------
    public async Task<MySubscriptionResponse> GetMySubscriptionAsync(Guid userId)
    {
        var result = new MySubscriptionResponse();

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        result.Current = await GetActiveSubscriptionAsync(conn, userId);
        result.Usage = await GetMonthlyUsageAsync(conn, userId, result.Current.Plan);
        result.BillingHistory = await GetBillingHistoryAsync(conn, userId);

        return result;
    }

    // ------------------------------------------------------------------
    private async Task<SubscriptionDto> GetActiveSubscriptionAsync(NpgsqlConnection conn, Guid userId)
    {
        const string sql = @"
            SELECT id, plan, status, amount_paise, currency,
                   started_at, expires_at, cancelled_at, auto_renew
            FROM subscriptions
            WHERE user_id = @userId AND status = 'active'
            ORDER BY started_at DESC
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            return new SubscriptionDto
            {
                Id = r.GetGuid(0),
                Plan = r.GetString(1),
                Status = r.GetString(2),
                AmountPaise = r.IsDBNull(3) ? null : r.GetInt32(3),
                Currency = r.IsDBNull(4) ? "INR" : r.GetString(4),
                StartedAt = r.GetDateTime(5),
                ExpiresAt = r.IsDBNull(6) ? null : r.GetDateTime(6),
                CancelledAt = r.IsDBNull(7) ? null : r.GetDateTime(7),
                AutoRenew = r.GetBoolean(8)
            };
        }

        // No active row → treat as free
        return new SubscriptionDto
        {
            Plan = "free",
            Status = "active",
            StartedAt = DateTime.UtcNow
        };
    }

    // ------------------------------------------------------------------
    private async Task<UsageDto> GetMonthlyUsageAsync(NpgsqlConnection conn, Guid userId, string plan)
    {
        const string sql = @"
            SELECT 
                COALESCE(SUM(total_questions), 0) AS questions_attempted,
                COUNT(*)                          AS quizzes_taken
            FROM test_attempts
            WHERE user_id = @userId
              AND completed_at >= date_trunc('month', CURRENT_DATE)
              AND completed_at IS NOT NULL";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        int questionsUsed = 0;
        int quizzesTaken = 0;

        await using var r = await cmd.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            questionsUsed = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetInt64(0));
            quizzesTaken = r.GetInt32(1);
        }

        var allowed = PlanQuestionLimits.GetValueOrDefault(plan, 100);

        return new UsageDto
        {
            QuestionsUsed = questionsUsed,
            QuestionsAllowed = allowed,
            QuizzesTakenThisMonth = quizzesTaken
        };
    }

    // ------------------------------------------------------------------
    private async Task<List<BillingHistoryItemDto>> GetBillingHistoryAsync(NpgsqlConnection conn, Guid userId)
    {
        const string sql = @"
            SELECT id, started_at, plan, amount_paise, status
            FROM subscriptions
            WHERE user_id = @userId
              AND plan <> 'free'
            ORDER BY started_at DESC
            LIMIT 20";

        var list = new List<BillingHistoryItemDto>();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var amountPaise = r.IsDBNull(3) ? 0 : r.GetInt32(3);
            list.Add(new BillingHistoryItemDto
            {
                Id = r.GetGuid(0),
                Date = r.GetDateTime(1),
                Plan = r.GetString(2),
                Amount = $"₹{amountPaise}",
                Status = r.GetString(4) == "active" || r.GetString(4) == "expired"
                            ? "Paid"
                            : r.GetString(4)
            });
        }

        return list;
    }

    // ------------------------------------------------------------------
    // Create (payment will be added later)
    // ------------------------------------------------------------------
    public async Task<(bool Success, string? Error, SubscriptionDto? Data)> CreateSubscriptionAsync(
        Guid userId, string plan, int amountPaise)
    {
        if (plan is not ("pro" or "elite"))
            return (false, "Invalid plan", null);

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        // 1. Cancel any existing active subscription
        const string cancelSql = @"
            UPDATE subscriptions
            SET status = 'cancelled', cancelled_at = now(), updated_at = now()
            WHERE user_id = @userId AND status = 'active'";

        await using (var cmd = new NpgsqlCommand(cancelSql, conn))
        {
            cmd.Parameters.AddWithValue("userId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Insert new subscription (1 year for now)
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

        await using var insertCmd = new NpgsqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("userId", userId);
        insertCmd.Parameters.AddWithValue("plan", plan);
        insertCmd.Parameters.AddWithValue("amount", amountPaise);

        await using var r = await insertCmd.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            var dto = new SubscriptionDto
            {
                Id = r.GetGuid(0),
                Plan = r.GetString(1),
                Status = r.GetString(2),
                AmountPaise = r.IsDBNull(3) ? null : r.GetInt32(3),
                Currency = r.GetString(4),
                StartedAt = r.GetDateTime(5),
                ExpiresAt = r.IsDBNull(6) ? null : r.GetDateTime(6),
                CancelledAt = r.IsDBNull(7) ? null : r.GetDateTime(7),
                AutoRenew = r.GetBoolean(8)
            };
            return (true, null, dto);
        }

        return (false, "Failed to create subscription", null);
    }

    // ------------------------------------------------------------------
    // Cancel
    // ------------------------------------------------------------------
    public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        const string sql = @"
            UPDATE subscriptions
            SET status = 'cancelled',
                cancelled_at = now(),
                auto_renew = false,
                updated_at = now()
            WHERE user_id = @userId AND status = 'active'";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? (true, null)
            : (false, "No active subscription found");
    }

    public async Task<(bool Allowed, string? Message)> CanUserTakeTestAsync(Guid userId)
{
    await using var conn = new NpgsqlConnection(_cs);
    await conn.OpenAsync();

    // 1. Get current plan + status
    var sub = await GetActiveSubscriptionAsync(conn, userId);   // re-use existing method

    // 2. If paid plan is expired → treat as free
    if (sub.Plan != "free" && sub.ExpiresAt.HasValue && sub.ExpiresAt < DateTime.UtcNow)
    {
        // optionally mark it expired here (or let the background job do it)
        sub.Plan = "free";
    }

    // 3. Free tier → check monthly limit
    if (sub.Plan == "free")
    {
        const string usageSql = @"
            SELECT COALESCE(SUM(total_questions), 0)
            FROM test_attempts
            WHERE user_id = @userId
              AND completed_at >= date_trunc('month', CURRENT_DATE)";

        await using var cmd = new NpgsqlCommand(usageSql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        var used = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

        if (used >= 100)
            return (false, "You have reached the free limit of 100 questions this month. Please upgrade to continue.");
    }

    // Pro / Elite → unlimited (as long as not expired)
    return (true, null);
}
}