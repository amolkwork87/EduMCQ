using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduMcq.Services;

public class OtpService
{
    private readonly IOtpRepository _otpRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IOtpRepository otpRepository, IConfiguration config, ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? DebugOtp)> SendOtpAsync(string phone, string purpose)
    {
        phone = NormalizePhone(phone);
        if (await _otpRepository.CountRecentOtpsAsync(phone) >= 5)
            return (false, "Too many OTP requests. Try again after some time.", null);

        var otp = Random.Shared.Next(100000, 999999).ToString();
        var expiryMinutes = _config.GetValue<int>("Otp:ExpiryMinutes", 10);
        await _otpRepository.StoreOtpAsync(phone, otp, purpose, DateTime.UtcNow.AddMinutes(expiryMinutes));
        _logger.LogWarning("OTP for {Phone}: {Otp} (valid {Minutes} min)", phone, otp, expiryMinutes);
        return (true, "OTP sent successfully", otp);
    }

    public async Task<(bool Success, string Message, Guid? UserId)> VerifyOtpAsync(string phone, string otp, string purpose)
    {
        phone = NormalizePhone(phone);
        var stored = await _otpRepository.GetLatestOtpAsync(phone, purpose);
        if (!stored.HasValue) return (false, "OTP not found or already used", null);
        if (stored.Value.IsUsed) return (false, "OTP already used", null);
        if (DateTime.UtcNow > stored.Value.ExpiresAt) return (false, "OTP has expired", null);
        if (stored.Value.Attempts >= _config.GetValue<int>("Otp:MaxAttempts", 5))
            return (false, "Too many failed attempts. Request a new OTP.", null);

        await _otpRepository.IncrementOtpAttemptsAsync(stored.Value.Id);
        if (stored.Value.Code != otp) return (false, "Invalid OTP", null);
        await _otpRepository.MarkOtpUsedAsync(stored.Value.Id);
        var userId = await _otpRepository.FindOrCreateUserByPhoneAsync(phone);
        return (true, "OTP verified successfully", userId);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId) => await _otpRepository.GetActiveUserByIdAsync(userId);

    public Task SaveRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt, string? createdByIp = null) =>
        _otpRepository.SaveRefreshTokenAsync(userId, refreshToken, expiresAt, createdByIp);

    public async Task<(bool IsValid, Guid? UserId)> ValidateRefreshTokenAsync(string refreshToken)
    {
        var token = await _otpRepository.GetRefreshTokenAsync(refreshToken);
        if (!token.HasValue || token.Value.RevokedAt.HasValue || DateTime.UtcNow > token.Value.ExpiresAt)
            return (false, null);
        return (true, token.Value.UserId);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken, string? revokedByIp = null) =>
        _otpRepository.RevokeRefreshTokenAsync(refreshToken, revokedByIp);

    public Task RevokeAllRefreshTokensAsync(Guid userId) => _otpRepository.RevokeAllRefreshTokensAsync(userId);

    public async Task<(bool Success, string Message, User? User)> VerifyOtpAndGetUserAsync(string phone, string otp, string purpose)
    {
        var (success, message, userId) = await VerifyOtpAsync(phone, otp, purpose);
        if (!success || userId == null) return (false, message, null);
        var user = await GetUserByIdAsync(userId.Value);
        return user == null ? (false, "User not found after OTP verification", null) : (true, "OTP verified successfully", user);
    }

    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+91")) return phone;
        if (phone.StartsWith("91") && phone.Length == 12) return "+" + phone;
        return phone.Length == 10 ? "+91" + phone : phone;
    }
}
