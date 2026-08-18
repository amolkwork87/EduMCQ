namespace EduMcq.Services;

using System.Data;
using System.Security.Cryptography;
using System.Text;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;                          // NuGet: Npgsql
using BCrypt.Net;                      // NuGet: BCrypt.Net-Next


public class UserService
{
    private readonly string _connectionString;
    private readonly JwtService _jwtService;
    // private readonly IEmailService _emailService;   // TODO: inject later
    // private readonly ISmsService _smsService;       // TODO: inject later

    public UserService(IConfiguration config, JwtService jwtService)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
        _jwtService = jwtService;
    }

    // =====================================================
    // FORGOT / RESET PASSWORD
    // =====================================================

    public async Task ForgotPasswordAsync(string email)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Find active user
        const string findSql = @"
            SELECT id FROM users 
            WHERE email = @email AND is_active = true";
        
        Guid? userId = null;
        await using (var cmd = new NpgsqlCommand(findSql, conn))
        {
            cmd.Parameters.AddWithValue("email", email);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null) return; // silent – prevent enumeration
            userId = (Guid)result;
        }

        // 2. Generate secure token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "").Replace("/", "").Replace("=", "");

        // 3. Insert into otp_verifications
        const string insertSql = @"
            INSERT INTO otp_verifications 
                (user_id, email, otp_code, purpose, expires_at, is_used, attempts)
            VALUES 
                (@userId, @email, @token, 'password_reset', @expires, false, 0)";

        await using (var cmd = new NpgsqlCommand(insertSql, conn))
        {
            cmd.Parameters.AddWithValue("userId", userId.Value);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("token", token);
            cmd.Parameters.AddWithValue("expires", DateTime.UtcNow.AddMinutes(30));
            await cmd.ExecuteNonQueryAsync();
        }

        // 4. Send email (placeholder)
        // await _emailService.SendPasswordResetAsync(email, token);
        // Link example: https://yourapp.com/forgot-password?token={token}
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Validate token
        const string findSql = @"
            SELECT id, user_id 
            FROM otp_verifications 
            WHERE otp_code = @token 
              AND purpose = 'password_reset' 
              AND is_used = false 
              AND expires_at > NOW()";

        Guid otpId;
        Guid userId;

        await using (var cmd = new NpgsqlCommand(findSql, conn))
        {
            cmd.Parameters.AddWithValue("token", token);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (false, "Invalid or expired reset link");

            otpId = reader.GetGuid(0);
            userId = reader.GetGuid(1);
        }

        // 2. Hash password
        var passwordHash = BCrypt.HashPassword(newPassword);

        // 3. Update password + mark OTP used (transaction)
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // Update users (you need a password_hash column!)
            const string updateUser = @"
                UPDATE users 
                SET password_hash = @hash, updated_at = NOW() 
                WHERE id = @userId";

            await using (var cmd = new NpgsqlCommand(updateUser, conn, tx))
            {
                cmd.Parameters.AddWithValue("hash", passwordHash);
                cmd.Parameters.AddWithValue("userId", userId);
                await cmd.ExecuteNonQueryAsync();
            }

            // Mark OTP used
            const string updateOtp = @"
                UPDATE otp_verifications 
                SET is_used = true 
                WHERE id = @otpId";

            await using (var cmd = new NpgsqlCommand(updateOtp, conn, tx))
            {
                cmd.Parameters.AddWithValue("otpId", otpId);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, null);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // =====================================================
    // OTP (SignupPage)
    // =====================================================

    // public async Task<(bool Success, string? Error)> SendOtpAsync(string phone, string purpose)
    // {
    //     if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
    //         return (false, "Invalid phone number");

    //     var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

    //     await using var conn = new NpgsqlConnection(_connectionString);
    //     await conn.OpenAsync();

    //     const string sql = @"
    //         INSERT INTO otp_verifications 
    //             (phone, otp_code, purpose, expires_at, is_used, attempts)
    //         VALUES 
    //             (@phone, @otp, @purpose, @expires, false, 0)";

    //     await using var cmd = new NpgsqlCommand(sql, conn);
    //     cmd.Parameters.AddWithValue("phone", phone);
    //     cmd.Parameters.AddWithValue("otp", otp);
    //     cmd.Parameters.AddWithValue("purpose", purpose);
    //     cmd.Parameters.AddWithValue("expires", DateTime.UtcNow.AddMinutes(10));
    //     await cmd.ExecuteNonQueryAsync();

    //     // TODO: send SMS
    //     // await _smsService.SendAsync(phone, $"Your OTP is {otp}");

    //     return (true, null);
    // }

    // public async Task<(bool Success, string? Error)> VerifyOtpAsync(string phone, string otp, string purpose)
    // {
    //     await using var conn = new NpgsqlConnection(_connectionString);
    //     await conn.OpenAsync();

    //     const string sql = @"
    //         SELECT id, attempts 
    //         FROM otp_verifications 
    //         WHERE phone = @phone 
    //           AND otp_code = @otp 
    //           AND purpose = @purpose 
    //           AND is_used = false 
    //           AND expires_at > NOW()
    //         ORDER BY created_at DESC
    //         LIMIT 1";

    //     await using var cmd = new NpgsqlCommand(sql, conn);
    //     cmd.Parameters.AddWithValue("phone", phone);
    //     cmd.Parameters.AddWithValue("otp", otp);
    //     cmd.Parameters.AddWithValue("purpose", purpose);

    //     await using var reader = await cmd.ExecuteReaderAsync();
    //     if (!await reader.ReadAsync())
    //         return (false, "Invalid or expired OTP");

    //     var otpId = reader.GetGuid(0);
    //     var attempts = reader.GetInt32(1);

    //     if (attempts >= 5)
    //         return (false, "Too many attempts. Please request a new OTP");

    //     // Mark as used
    //     await reader.CloseAsync();

    //     const string updateSql = @"
    //         UPDATE otp_verifications 
    //         SET is_used = true, attempts = attempts + 1 
    //         WHERE id = @id";

    //     await using var updateCmd = new NpgsqlCommand(updateSql, conn);
    //     updateCmd.Parameters.AddWithValue("id", otpId);
    //     await updateCmd.ExecuteNonQueryAsync();

    //     return (true, null);
    // }

    // =====================================================
    // SIGNUP
    // =====================================================

    public async Task<(bool Success, string? Error, object? Data)> SignupAsync(SignupRequest req)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Uniqueness checks
        if (await EmailExistsAsync(conn, req.Email))
            return (false, "Email already registered", null);

        if (!string.IsNullOrEmpty(req.Phone) && await PhoneExistsAsync(conn, req.Phone))
            return (false, "Phone already registered", null);

        // Phone must be verified if provided
        if (!string.IsNullOrEmpty(req.Phone))
        {
            var verified = await IsPhoneRecentlyVerifiedAsync(conn, req.Phone, "signup");
            if (!verified)
                return (false, "Please verify your phone number first", null);
        }

        var passwordHash = BCrypt.HashPassword(req.Password);
        var userId = Guid.NewGuid();
        var fullName = $"{req.FirstName} {req.LastName}".Trim();

        const string insertSql = @"
            INSERT INTO users 
                (id, email, phone, name, role, class_standard, school_name, 
                 is_phone_verified, is_email_verified, is_active, password_hash, created_at, updated_at)
            VALUES 
                (@id, @email, @phone, @name, @role, @class, @school, 
                 @phoneVerified, false, true, @hash, NOW(), NOW())";

        await using var cmd = new NpgsqlCommand(insertSql, conn);
        cmd.Parameters.AddWithValue("id", userId);
        cmd.Parameters.AddWithValue("email", req.Email);
        cmd.Parameters.AddWithValue("phone", (object?)req.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("name", fullName);
        cmd.Parameters.AddWithValue("role", req.Role.ToLower());
        cmd.Parameters.AddWithValue("class", (object?)req.ClassStandard ?? DBNull.Value);
        cmd.Parameters.AddWithValue("school", (object?)req.SchoolName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("phoneVerified", !string.IsNullOrEmpty(req.Phone));
        cmd.Parameters.AddWithValue("hash", passwordHash);

        await cmd.ExecuteNonQueryAsync();

        // Optional: create free subscription
        // await CreateFreeSubscriptionAsync(conn, userId);

        // Build user object for JWT
        var user = new User
        {
            UserId = userId,
            Email = req.Email,
            MobileNumber = req.Phone,
            Name = fullName,
            Role = req.Role.ToLower(),
            Status = "active"
        };

        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);

        return (true, null, new
        {
            user = new { user.UserId, user.Name, user.Email, user.Role },
            accessToken,
            refreshToken
        });
    }

    // =====================================================
    // SUBSCRIPTIONS (moved here)
    // =====================================================

    public async Task<object?> GetActiveSubscriptionAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT id, plan, status, amount_paise, currency, started_at, expires_at, auto_renew
            FROM subscriptions
            WHERE user_id = @userId 
              AND status = 'active' 
              AND expires_at > NOW()
            ORDER BY expires_at DESC
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new
        {
            Id = reader.GetGuid(0),
            Plan = reader.GetString(1),
            Status = reader.GetString(2),
            AmountPaise = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
            Currency = reader.GetString(4),
            StartedAt = reader.GetDateTime(5),
            ExpiresAt = reader.GetDateTime(6),
            AutoRenew = reader.GetBoolean(7)
        };
    }

    public async Task<(bool Success, string? Error, object? Data)> CreateSubscriptionAsync(
        Guid userId, string plan, int amountPaise)
    {
        // TODO: Call Razorpay API here to create subscription
        // For now just insert a pending record

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var subId = Guid.NewGuid();

        const string sql = @"
            INSERT INTO subscriptions 
                (id, user_id, plan, status, amount_paise, currency, started_at, expires_at, auto_renew)
            VALUES 
                (@id, @userId, @plan, 'pending', @amount, 'INR', NOW(), NOW() + INTERVAL '30 days', true)";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", subId);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("plan", plan);
        cmd.Parameters.AddWithValue("amount", amountPaise);
        await cmd.ExecuteNonQueryAsync();

        return (true, null, new { SubscriptionId = subId, Status = "pending" });
    }

    public async Task HandleRazorpayWebhookAsync(string eventType, string razorpaySubId, string status)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
            UPDATE subscriptions 
            SET status = @status,
                razorpay_subscription_id = @razorpayId,
                updated_at = NOW()          -- add this column if missing
            WHERE razorpay_subscription_id = @razorpayId 
               OR id = @razorpayId::uuid";  // adjust as needed

        // Real implementation should parse the full webhook payload
        // and map Razorpay statuses correctly.
    }

    // =====================================================
    // Private helpers
    // =====================================================

    private async Task<bool> EmailExistsAsync(NpgsqlConnection conn, string email)
    {
        const string sql = "SELECT 1 FROM users WHERE email = @email LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("email", email);
        return await cmd.ExecuteScalarAsync() != null;
    }

    private async Task<bool> PhoneExistsAsync(NpgsqlConnection conn, string phone)
    {
        const string sql = "SELECT 1 FROM users WHERE phone = @phone LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("phone", phone);
        return await cmd.ExecuteScalarAsync() != null;
    }

    private async Task<bool> IsPhoneRecentlyVerifiedAsync(NpgsqlConnection conn, string phone, string purpose)
    {
        const string sql = @"
            SELECT 1 FROM otp_verifications 
            WHERE phone = @phone 
              AND purpose = @purpose 
              AND is_used = true 
              AND expires_at > NOW() - INTERVAL '15 minutes'
            LIMIT 1";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("phone", phone);
        cmd.Parameters.AddWithValue("purpose", purpose);
        return await cmd.ExecuteScalarAsync() != null;
    }
}
