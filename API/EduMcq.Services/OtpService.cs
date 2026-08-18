using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace EduMcq.Services;

public class OtpService
{
    private readonly string _connectionString;
    private readonly IConfiguration _config;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IConfiguration config, ILogger<OtpService> logger)
    {
        _connectionString = config["EduMcqDatabaseConnection"]!;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? DebugOtp)> SendOtpAsync(string phone, string purpose)
    {
        phone = NormalizePhone(phone);   // force +91... format

        // 1. Rate limiting (simple version)
        if (await IsRateLimited(phone))
            return (false, "Too many OTP requests. Try again after some time.", null);

        // 2. Generate OTP
        var otp = GenerateOtp();
        var expiryMinutes = _config.GetValue<int>("Otp:ExpiryMinutes", 10);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // 3. Store in otp_verifications
        await using var conn = new NpgsqlConnection(_connectionString); // or SqlConnection
        await conn.OpenAsync();

        // Optional: invalidate previous unused OTPs for same phone + purpose
        await using (var cmd = new NpgsqlCommand(@"
            UPDATE otp_verifications 
            SET is_used = true 
            WHERE phone = @phone AND purpose = @purpose AND is_used = false", conn))
        {
            cmd.Parameters.AddWithValue("phone", phone);
            cmd.Parameters.AddWithValue("purpose", purpose);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(@"
            INSERT INTO otp_verifications (phone, otp_code, purpose, expires_at)
            VALUES (@phone, @otp, @purpose, @expiresAt)", conn))
        {
            cmd.Parameters.AddWithValue("phone", phone);
            cmd.Parameters.AddWithValue("otp", otp);
            cmd.Parameters.AddWithValue("purpose", purpose);
            cmd.Parameters.AddWithValue("expiresAt", expiresAt);
            await cmd.ExecuteNonQueryAsync();
        }

        // 4. Send SMS
        string? debugOtp = null;
        var twilioEnabled = _config.GetValue<bool>("Twilio:Enabled");

        // if (twilioEnabled )//&& !_env.IsDevelopment())
        // {
        //     await SendSmsViaTwilio(phone, otp);
        // }
        // else
        {
            // Local / Development mode
            _logger.LogWarning("OTP for {Phone}: {Otp} (valid {Minutes} min)", phone, otp, expiryMinutes);
            debugOtp = otp;   // return only in Development
        }

        return (true, "OTP sent successfully", debugOtp);
    }

    public async Task<(bool Success, string Message, Guid? UserId)> VerifyOtpAsync(string phone, string otp, string purpose)
    {
        phone = NormalizePhone(phone);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Get latest valid OTP
        await using var cmd = new NpgsqlCommand(@"
            SELECT id, otp_code, expires_at, is_used, attempts
            FROM otp_verifications
            WHERE phone = @phone AND purpose = @purpose AND is_used = false
            ORDER BY created_at DESC
            LIMIT 1", conn);

        cmd.Parameters.AddWithValue("phone", phone);
        cmd.Parameters.AddWithValue("purpose", purpose);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return (false, "OTP not found or already used", null);

        var otpId = reader.GetGuid(0);
        var storedOtp = reader.GetString(1);
        var expiresAt = reader.GetDateTime(2);
        var isUsed = reader.GetBoolean(3);
        var attempts = reader.GetInt32(4);

        await reader.CloseAsync();

        // 2. Validations
        if (isUsed)
            return (false, "OTP already used", null);

        if (DateTime.UtcNow > expiresAt)
            return (false, "OTP has expired", null);

        var maxAttempts = _config.GetValue<int>("Otp:MaxAttempts", 5);
        if (attempts >= maxAttempts)
            return (false, "Too many failed attempts. Request a new OTP.", null);

        // 3. Increment attempts
        await using (var updateAttempts = new NpgsqlCommand(
            "UPDATE otp_verifications SET attempts = attempts + 1 WHERE id = @id", conn))
        {
            updateAttempts.Parameters.AddWithValue("id", otpId);
            await updateAttempts.ExecuteNonQueryAsync();
        }

        if (storedOtp != otp)
            return (false, "Invalid OTP", null);

        // 4. Mark as used
        await using (var markUsed = new NpgsqlCommand(
            "UPDATE otp_verifications SET is_used = true WHERE id = @id", conn))
        {
            markUsed.Parameters.AddWithValue("id", otpId);
            await markUsed.ExecuteNonQueryAsync();
        }

        // 5. Find or create user
        var userId = await FindOrCreateUserByPhone(conn, phone);

        return (true, "OTP verified successfully", userId);
    }

    // ---------- Helpers ----------

    private string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    private string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+91")) return phone;
        if (phone.StartsWith("91") && phone.Length == 12) return "+" + phone;
        if (phone.Length == 10) return "+91" + phone;
        return phone; // already in E.164 hopefully
    }

    private async Task SendSmsViaTwilio(string toPhone, string otp)
    {
        var accountSid = _config["Twilio:AccountSid"];
        var authToken = _config["Twilio:AuthToken"];
        var fromNumber = _config["Twilio:FromPhoneNumber"];

        TwilioClient.Init(accountSid, authToken);

        try
        {
            var message = await MessageResource.CreateAsync(
            body: $"Your EduMCQ login code is {otp}. Valid for {_config.GetValue<int>("Otp:ExpiryMinutes")} minutes. Do not share.",
            from: new Twilio.Types.PhoneNumber(fromNumber),
            to: new Twilio.Types.PhoneNumber(toPhone)
                );

            _logger.LogInformation("Twilio message SID: {Sid}", message.Sid);
        }
        catch (System.Exception ex)
        {
            _logger.LogError("Twilio message SID: {Sid}", ex);
            throw;
        }

    }

    private async Task<bool> IsRateLimited(string phone)
    {
        // Simple: max 5 OTPs in last 1 hour
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM otp_verifications
            WHERE phone = @phone AND created_at > NOW() - INTERVAL '1 hour'", conn);
        cmd.Parameters.AddWithValue("phone", phone);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count >= 5;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
        SELECT id, phone, name, role, is_phone_verified,is_email_verified,avatar_url
        FROM users 
        WHERE id = @id AND is_active = true", conn);

        cmd.Parameters.AddWithValue("id", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new User
        {
            UserId = reader.GetGuid(0),
            MobileNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
            Name = reader.IsDBNull(2) ? null : reader.GetString(2),
            Role = reader.IsDBNull(3) ? "student" : reader.GetString(3),
            Status = reader.IsDBNull(4) ? "active" : reader.GetString(4)
            // add other fields if needed
        };
    }

    private async Task<Guid> FindOrCreateUserByPhone(NpgsqlConnection conn, string phone)
    {
        // Try find
        await using (var cmd = new NpgsqlCommand(
            "SELECT id FROM users WHERE phone = @phone", conn))
        {
            cmd.Parameters.AddWithValue("phone", phone);
            var existing = await cmd.ExecuteScalarAsync();
            if (existing != null)
            {
                var userId = (Guid)existing;
                // Update last_login etc.
                await using var update = new NpgsqlCommand(@"
                    UPDATE users 
                    SET is_phone_verified = true, 
                        last_login_at = NOW(), 
                        login_count = login_count + 1,
                        updated_at = NOW()
                    WHERE id = @id", conn);
                update.Parameters.AddWithValue("id", userId);
                await update.ExecuteNonQueryAsync();
                return userId;
            }
        }

        // Create new user
        var newId = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand(@"
            INSERT INTO users (id, phone, is_phone_verified, role, created_at, updated_at)
            VALUES (@id, @phone, true, 'student', NOW(), NOW())", conn))
        {
            insert.Parameters.AddWithValue("id", newId);
            insert.Parameters.AddWithValue("phone", phone);
            await insert.ExecuteNonQueryAsync();
        }
        return newId;
    }

    public async Task SaveRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt, string? createdByIp = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
        INSERT INTO refresh_tokens (user_id, token, expires_at, created_by_ip)
        VALUES (@userId, @token, @expiresAt, @ip)", conn);

        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("token", refreshToken);
        cmd.Parameters.AddWithValue("expiresAt", expiresAt);
        cmd.Parameters.AddWithValue("ip", (object?)createdByIp ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(bool IsValid, Guid? UserId)> ValidateRefreshTokenAsync(string refreshToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
        SELECT user_id, expires_at, revoked_at
        FROM refresh_tokens
        WHERE token = @token", conn);

        cmd.Parameters.AddWithValue("token", refreshToken);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return (false, null);

        var userId = reader.GetGuid(0);
        var expiresAt = reader.GetDateTime(1);
        var revokedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);

        if (revokedAt.HasValue || DateTime.UtcNow > expiresAt)
            return (false, null);

        return (true, userId);
    }


    public async Task RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
        UPDATE refresh_tokens
        SET revoked_at = NOW(),
            revoked_by_ip = @ip
        WHERE token = @token AND revoked_at IS NULL", conn);

        cmd.Parameters.AddWithValue("token", refreshToken);
        cmd.Parameters.AddWithValue("ip", (object?)revokedByIp ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task RevokeAllRefreshTokensAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
        UPDATE refresh_tokens
        SET revoked_at = NOW()
        WHERE user_id = @userId AND revoked_at IS NULL", conn);

        cmd.Parameters.AddWithValue("userId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(bool Success, string Message, User? User)> VerifyOtpAndGetUserAsync(
    string phone,
    string otp,
    string purpose)
    {
        // 1. First verify the OTP (reuses existing logic)
        var (success, message, userId) = await VerifyOtpAsync(phone, otp, purpose);

        if (!success || userId == null)
        {
            return (false, message, null);
        }

        // 2. Get the full user object
        var user = await GetUserByIdAsync(userId.Value);

        if (user == null)
        {
            return (false, "User not found after OTP verification", null);
        }

        return (true, "OTP verified successfully", user);
    }
}