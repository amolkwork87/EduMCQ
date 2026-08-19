using System.Security.Cryptography;
using EduMcq.Interfaces;
using EduMcq.Models;
using BCrypt.Net;

namespace EduMcq.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;

    public UserService(IUserRepository userRepository, JwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var userId = await _userRepository.GetActiveUserIdByEmailAsync(email);
        if (!userId.HasValue) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "").Replace("/", "").Replace("=", "");
        await _userRepository.CreatePasswordResetAsync(userId.Value, email, token, DateTime.UtcNow.AddMinutes(30));
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string token, string newPassword)
    {
        var reset = await _userRepository.GetValidPasswordResetAsync(token);
        if (!reset.HasValue) return (false, "Invalid or expired reset link");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        var updated = await _userRepository.ResetPasswordAsync(reset.Value.OtpId, reset.Value.UserId, passwordHash);
        return updated ? (true, null) : (false, "Unable to reset password");
    }

    public async Task<(bool Success, string? Error, object? Data)> SignupAsync(SignupRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            return (false, "Email already registered", null);
        if (!string.IsNullOrEmpty(request.Phone) && await _userRepository.PhoneExistsAsync(request.Phone))
            return (false, "Phone already registered", null);
        if (!string.IsNullOrEmpty(request.Phone) &&
            !await _userRepository.IsPhoneRecentlyVerifiedAsync(request.Phone, "signup"))
            return (false, "Please verify your phone number first", null);

        var user = await _userRepository.CreateUserAsync(request, BCrypt.Net.BCrypt.HashPassword(request.Password));
        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);
        return (true, null, new
        {
            user = new { user.UserId, user.Name, user.Email, user.Role },
            accessToken,
            refreshToken
        });
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid userId) =>
        await _userRepository.GetActiveSubscriptionAsync(userId);

    public async Task<(bool Success, string? Error, object? Data)> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise)
    {
        var subscriptionId = await _userRepository.CreateSubscriptionAsync(userId, plan, amountPaise);
        return (true, null, new { SubscriptionId = subscriptionId, Status = "pending" });
    }

    public Task HandleRazorpayWebhookAsync(string eventType, string razorpaySubId, string status) =>
        _userRepository.HandleRazorpayWebhookAsync(eventType, razorpaySubId, status);
}
