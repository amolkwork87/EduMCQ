using System.Net;
using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

namespace EduMcq.Repository;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration["EduMcqDatabaseConnection"]
            ?? throw new InvalidOperationException("EduMcqDatabaseConnection is not configured.");
    }

    public async Task<User?> GetActiveUserByEmailAsync(string email)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT id, phone, email, name, role, avatar_url FROM users WHERE email = @email AND is_active = true", connection);
        command.Parameters.AddWithValue("email", email);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    public async Task<Guid?> GetActiveUserIdByEmailAsync(string email)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT id FROM users WHERE email = @email AND is_active = true", connection);
        command.Parameters.AddWithValue("email", email);
        var result = await command.ExecuteScalarAsync();
        return result is Guid id ? id : null;
    }

    public async Task CreatePasswordResetAsync(Guid userId, string email, string token, DateTime expiresAt)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"INSERT INTO otp_verifications (user_id, email, otp_code, purpose, expires_at, is_used, attempts)
            VALUES (@userId, @email, @token, 'password_reset', @expiresAt, false, 0)", connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<(Guid OtpId, Guid UserId)?> GetValidPasswordResetAsync(string token)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"SELECT id, user_id FROM otp_verifications
            WHERE otp_code = @token AND purpose = 'password_reset' AND is_used = false AND expires_at > NOW()", connection);
        command.Parameters.AddWithValue("token", token);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetGuid(0), reader.GetGuid(1)) : null;
    }

    public async Task<bool> ResetPasswordAsync(Guid otpId, Guid userId, string passwordHash)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var updateUser = new NpgsqlCommand("UPDATE users SET password_hash = @hash, updated_at = NOW() WHERE id = @userId", connection, transaction);
            updateUser.Parameters.AddWithValue("hash", passwordHash);
            updateUser.Parameters.AddWithValue("userId", userId);
            var updated = await updateUser.ExecuteNonQueryAsync();

            await using var updateOtp = new NpgsqlCommand("UPDATE otp_verifications SET is_used = true WHERE id = @otpId", connection, transaction);
            updateOtp.Parameters.AddWithValue("otpId", otpId);
            await updateOtp.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return updated > 0;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<bool> EmailExistsAsync(string email) => ExistsAsync("SELECT 1 FROM users WHERE email = @value LIMIT 1", email);
    public Task<bool> PhoneExistsAsync(string phone) => ExistsAsync("SELECT 1 FROM users WHERE phone = @value LIMIT 1", phone);

    public async Task<bool> IsPhoneRecentlyVerifiedAsync(string phone, string purpose)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"SELECT 1 FROM otp_verifications
            WHERE phone = @phone AND purpose = @purpose AND is_used = true
            AND expires_at > NOW() - INTERVAL '15 minutes' LIMIT 1", connection);
        command.Parameters.AddWithValue("phone", phone);
        command.Parameters.AddWithValue("purpose", purpose);
        return await command.ExecuteScalarAsync() != null;
    }

    public async Task<User> CreateUserAsync(SignupRequest request, string passwordHash)
    {
        var userId = Guid.NewGuid();
        var name = $"{request.FirstName} {request.LastName}".Trim();
        var role = request.Role.ToLowerInvariant();
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"INSERT INTO users
            (id, email, phone, name, role, class_standard, school_name, is_phone_verified,
             is_email_verified, is_active, password_hash, created_at, updated_at)
            VALUES (@id, @email, @phone, @name, @role, @classStandard, @schoolName,
                    @phoneVerified, false, true, @passwordHash, NOW(), NOW())", connection);
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("email", request.Email);
        command.Parameters.AddWithValue("phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("classStandard", (object?)request.ClassStandard ?? DBNull.Value);
        command.Parameters.AddWithValue("schoolName", (object?)request.SchoolName ?? DBNull.Value);
        command.Parameters.AddWithValue("phoneVerified", !string.IsNullOrEmpty(request.Phone));
        command.Parameters.AddWithValue("passwordHash", passwordHash);
        await command.ExecuteNonQueryAsync();
        return new User { UserId = userId, Email = request.Email, MobileNumber = request.Phone, Name = name, Role = role, Status = "active" };
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"SELECT id, user_id, plan, status, amount_paise, currency, started_at, expires_at, auto_renew
            FROM subscriptions WHERE user_id = @userId AND status = 'active' AND expires_at > NOW()
            ORDER BY expires_at DESC LIMIT 1", connection);
        command.Parameters.AddWithValue("userId", userId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new SubscriptionDto
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Plan = reader.GetString(2),
            Status = reader.GetString(3),
            AmountPaise = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            Currency = reader.GetString(5),
            StartedAt = reader.GetDateTime(6),
            ExpiresAt = reader.GetDateTime(7),
            AutoRenew = reader.GetBoolean(8)
        };
    }

    public async Task<Guid> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise)
    {
        var id = Guid.NewGuid();
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"INSERT INTO subscriptions
            (id, user_id, plan, status, amount_paise, currency, started_at, expires_at, auto_renew)
            VALUES (@id, @userId, @plan, 'pending', @amount, 'INR', NOW(), NOW() + INTERVAL '30 days', true)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("plan", plan);
        command.Parameters.AddWithValue("amount", amountPaise);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    public async Task HandleRazorpayWebhookAsync(string eventType, string razorpaySubscriptionId, string status)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"UPDATE subscriptions
            SET status = @status, razorpay_subscription_id = @razorpayId, updated_at = NOW()
            WHERE razorpay_subscription_id = @razorpayId OR id = @razorpayId::uuid", connection);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("razorpayId", razorpaySubscriptionId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<User> FindOrCreateSocialUserAsync(string provider, SocialUserInfo socialUser)
    {
        var normalizedProvider = provider.ToLowerInvariant();
        var column = normalizedProvider switch
        {
            "google" => "google_id",
            "facebook" => "facebook_id",
            "apple" => "apple_id",
            _ => throw new ArgumentException("Invalid provider", nameof(provider))
        };
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var user = await FindUserAsync(connection, transaction, $"{column} = @providerId", "providerId", socialUser.ProviderId);
            if (user != null) { await transaction.CommitAsync(); return user; }
            if (!string.IsNullOrEmpty(socialUser.Email))
            {
                user = await FindUserAsync(connection, transaction, "email = @email", "email", socialUser.Email);
                if (user != null)
                {
                    await using var link = new NpgsqlCommand($"UPDATE users SET {column} = @providerId, updated_at = NOW() WHERE id = @userId AND {column} IS NULL", connection, transaction);
                    link.Parameters.AddWithValue("providerId", socialUser.ProviderId);
                    link.Parameters.AddWithValue("userId", user.UserId);
                    await link.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    return user;
                }
            }
            var userId = Guid.NewGuid();
            await using var insert = new NpgsqlCommand($@"INSERT INTO users
                (id, email, name, {column}, avatar_url, is_email_verified, role, created_at, updated_at)
                VALUES (@id, @email, @name, @providerId, @avatar, @emailVerified, 'student', NOW(), NOW())", connection, transaction);
            insert.Parameters.AddWithValue("id", userId);
            insert.Parameters.AddWithValue("email", (object?)socialUser.Email ?? DBNull.Value);
            insert.Parameters.AddWithValue("name", (object?)socialUser.Name ?? DBNull.Value);
            insert.Parameters.AddWithValue("providerId", socialUser.ProviderId);
            insert.Parameters.AddWithValue("avatar", (object?)socialUser.Picture ?? DBNull.Value);
            insert.Parameters.AddWithValue("emailVerified", socialUser.EmailVerified);
            await insert.ExecuteNonQueryAsync();
            await using var socialInsert = new NpgsqlCommand(@"INSERT INTO social_logins
                (user_id, provider, provider_id, provider_email) VALUES (@userId, @provider, @providerId, @email)", connection, transaction);
            socialInsert.Parameters.AddWithValue("userId", userId);
            socialInsert.Parameters.AddWithValue("provider", normalizedProvider);
            socialInsert.Parameters.AddWithValue("providerId", socialUser.ProviderId);
            socialInsert.Parameters.AddWithValue("email", (object?)socialUser.Email ?? DBNull.Value);
            await socialInsert.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
            return new User { UserId = userId, Email = socialUser.Email, Name = socialUser.Name, Role = "student", Status = "active", AvatarUrl = socialUser.Picture };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAt, string? createdByIp)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("INSERT INTO refresh_tokens (user_id, token_hash, expires_at, ip_address) VALUES (@userId, @token, @expiresAt, @ip)", connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("expiresAt", expiresAt);
        if (IPAddress.TryParse(createdByIp, out var ipAddress))
        {
            command.Parameters.AddWithValue("ip", NpgsqlDbType.Inet, ipAddress);
        }
        else
        {
            command.Parameters.AddWithValue("ip", DBNull.Value);
        }
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> ExistsAsync(string sql, string value)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("value", value);
        return await command.ExecuteScalarAsync() != null;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<User?> FindUserAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string predicate, string parameterName, string value)
    {
        await using var command = new NpgsqlCommand($"SELECT id, phone, email, name, role, avatar_url FROM users WHERE {predicate}", connection, transaction);
        command.Parameters.AddWithValue(parameterName, value);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    private static User MapUser(NpgsqlDataReader reader) => new()
    {
        UserId = reader.GetGuid(0),
        MobileNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
        Email = reader.IsDBNull(2) ? null : reader.GetString(2),
        Name = reader.IsDBNull(3) ? null : reader.GetString(3),
        Role = reader.IsDBNull(4) ? "student" : reader.GetString(4),
        Status = "active",
        AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5)
    };
}
