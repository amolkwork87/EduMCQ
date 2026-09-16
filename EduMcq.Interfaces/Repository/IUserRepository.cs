namespace EduMcq.Interfaces;

using EduMcq.Models;

public interface IUserRepository
{
	Task<Guid?> GetActiveUserIdByEmailAsync(string email);
	Task<User?> GetActiveUserByEmailAsync(string email);
	Task CreatePasswordResetAsync(Guid userId, string email, string token, DateTime expiresAt);
	Task<(Guid OtpId, Guid UserId)?> GetValidPasswordResetAsync(string token);
	Task<bool> ResetPasswordAsync(Guid otpId, Guid userId, string passwordHash);
	Task<bool> EmailExistsAsync(string email);
	Task<bool> PhoneExistsAsync(string phone);
	Task<bool> IsPhoneRecentlyVerifiedAsync(string phone, string purpose);
	Task<User> CreateUserAsync(SignupRequest request, string passwordHash);
	Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId);
	Task<Guid> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise);
	Task HandleRazorpayWebhookAsync(string eventType, string razorpaySubscriptionId, string status);
	Task<User> FindOrCreateSocialUserAsync(string provider, SocialUserInfo socialUser);
	Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAt, string? createdByIp);

	Task<UserProfileDto?> GetByIdAsync(Guid userId);
	Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}
