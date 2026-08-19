using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EduMcq.Repository;

public class OtpRepository : IOtpRepository
{
    private readonly string _connectionString;

    public OtpRepository(IConfiguration configuration)
    {
        _connectionString = configuration["EduMcqDatabaseConnection"]
            ?? throw new InvalidOperationException("EduMcqDatabaseConnection is not configured.");
    }

    public async Task<int> CountRecentOtpsAsync(string phone)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM otp_verifications WHERE phone = @phone AND created_at > NOW() - INTERVAL '1 hour'", connection);
        command.Parameters.AddWithValue("phone", phone);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task StoreOtpAsync(string phone, string otp, string purpose, DateTime expiresAt)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var invalidate = new NpgsqlCommand("UPDATE otp_verifications SET is_used = true WHERE phone = @phone AND purpose = @purpose AND is_used = false", connection, transaction);
            invalidate.Parameters.AddWithValue("phone", phone); invalidate.Parameters.AddWithValue("purpose", purpose);
            await invalidate.ExecuteNonQueryAsync();
            await using var insert = new NpgsqlCommand("INSERT INTO otp_verifications (phone, otp_code, purpose, expires_at) VALUES (@phone, @otp, @purpose, @expiresAt)", connection, transaction);
            insert.Parameters.AddWithValue("phone", phone); insert.Parameters.AddWithValue("otp", otp); insert.Parameters.AddWithValue("purpose", purpose); insert.Parameters.AddWithValue("expiresAt", expiresAt);
            await insert.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<(Guid Id, string Code, DateTime ExpiresAt, bool IsUsed, int Attempts)?> GetLatestOtpAsync(string phone, string purpose)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(@"SELECT id, otp_code, expires_at, is_used, attempts FROM otp_verifications
            WHERE phone = @phone AND purpose = @purpose AND is_used = false ORDER BY created_at DESC LIMIT 1", connection);
        command.Parameters.AddWithValue("phone", phone); command.Parameters.AddWithValue("purpose", purpose);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? (reader.GetGuid(0), reader.GetString(1), reader.GetDateTime(2), reader.GetBoolean(3), reader.GetInt32(4))
            : null;
    }

    public Task IncrementOtpAttemptsAsync(Guid otpId) => ExecuteAsync("UPDATE otp_verifications SET attempts = attempts + 1 WHERE id = @id", "id", otpId);
    public Task MarkOtpUsedAsync(Guid otpId) => ExecuteAsync("UPDATE otp_verifications SET is_used = true WHERE id = @id", "id", otpId);

    public async Task<Guid> FindOrCreateUserByPhoneAsync(string phone)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var find = new NpgsqlCommand("SELECT id FROM users WHERE phone = @phone", connection, transaction);
            find.Parameters.AddWithValue("phone", phone);
            var existing = await find.ExecuteScalarAsync();
            if (existing is Guid existingId)
            {
                await using var update = new NpgsqlCommand("UPDATE users SET is_phone_verified = true, last_login_at = NOW(), login_count = login_count + 1, updated_at = NOW() WHERE id = @id", connection, transaction);
                update.Parameters.AddWithValue("id", existingId); await update.ExecuteNonQueryAsync();
                await transaction.CommitAsync(); return existingId;
            }
            var userId = Guid.NewGuid();
            await using var insert = new NpgsqlCommand("INSERT INTO users (id, phone, is_phone_verified, role, created_at, updated_at) VALUES (@id, @phone, true, 'student', NOW(), NOW())", connection, transaction);
            insert.Parameters.AddWithValue("id", userId); insert.Parameters.AddWithValue("phone", phone); await insert.ExecuteNonQueryAsync();
            await transaction.CommitAsync(); return userId;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<User?> GetActiveUserByIdAsync(Guid userId)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT id, phone, email, name, role, is_phone_verified, is_email_verified, avatar_url FROM users WHERE id = @id AND is_active = true", connection);
        command.Parameters.AddWithValue("id", userId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new User
        {
            UserId = reader.GetGuid(0), MobileNumber = reader.IsDBNull(1) ? null : reader.GetString(1), Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            Name = reader.IsDBNull(3) ? null : reader.GetString(3), Role = reader.IsDBNull(4) ? "student" : reader.GetString(4),
            IsPhoneVerified = !reader.IsDBNull(5) && reader.GetBoolean(5), IsEmailVerified = !reader.IsDBNull(6) && reader.GetBoolean(6),
            AvatarUrl = reader.IsDBNull(7) ? null : reader.GetString(7), Status = "active"
        };
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAt, string? createdByIp)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("INSERT INTO refresh_tokens (user_id, token, expires_at, created_by_ip) VALUES (@userId, @token, @expiresAt, @ip)", connection);
        command.Parameters.AddWithValue("userId", userId); command.Parameters.AddWithValue("token", token); command.Parameters.AddWithValue("expiresAt", expiresAt); command.Parameters.AddWithValue("ip", (object?)createdByIp ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<(Guid UserId, DateTime ExpiresAt, DateTime? RevokedAt)?> GetRefreshTokenAsync(string token)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT user_id, expires_at, revoked_at FROM refresh_tokens WHERE token = @token", connection);
        command.Parameters.AddWithValue("token", token);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetGuid(0), reader.GetDateTime(1), reader.IsDBNull(2) ? null : reader.GetDateTime(2)) : null;
    }

    public Task RevokeRefreshTokenAsync(string token, string? revokedByIp) => ExecuteAsync("UPDATE refresh_tokens SET revoked_at = NOW(), revoked_by_ip = @ip WHERE token = @token AND revoked_at IS NULL", ("token", token), ("ip", (object?)revokedByIp ?? DBNull.Value));
    public Task RevokeAllRefreshTokensAsync(Guid userId) => ExecuteAsync("UPDATE refresh_tokens SET revoked_at = NOW() WHERE user_id = @userId AND revoked_at IS NULL", "userId", userId);

    private async Task ExecuteAsync(string sql, string name, object value) => await ExecuteAsync(sql, (name, value));
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); return connection;
    }
}