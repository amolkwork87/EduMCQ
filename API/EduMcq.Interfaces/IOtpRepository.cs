using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface IOtpRepository
{
    Task<int> CountRecentOtpsAsync(string phone);
    Task StoreOtpAsync(string phone, string otp, string purpose, DateTime expiresAt);
    Task<(Guid Id, string Code, DateTime ExpiresAt, bool IsUsed, int Attempts)?> GetLatestOtpAsync(string phone, string purpose);
    Task IncrementOtpAttemptsAsync(Guid otpId);
    Task MarkOtpUsedAsync(Guid otpId);
    Task<Guid> FindOrCreateUserByPhoneAsync(string phone);
    Task<User?> GetActiveUserByIdAsync(Guid userId);
    Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAt, string? createdByIp);
    Task<(Guid UserId, DateTime ExpiresAt, DateTime? RevokedAt)?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? revokedByIp);
    Task RevokeAllRefreshTokensAsync(Guid userId);
}