using System.Security.Cryptography;
using EduMcq.Interfaces;
using EduMcq.Models;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduMcq.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;

    private readonly IConfiguration _config;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, JwtService jwtService,IConfiguration config, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _config = config;
        _logger = logger;
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
        // if (!string.IsNullOrEmpty(request.Phone) &&
        //     !await _userRepository.IsPhoneRecentlyVerifiedAsync(request.Phone, "signup"))
        //     return (false, "Please verify your phone number first", null);

        var user = await _userRepository.CreateUserAsync(request, BCrypt.Net.BCrypt.HashPassword(request.Password));
        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);
        return (true, null, new
        {
            user = new { user.UserId, user.Name, user.Email, user.Role },
            accessToken,
            refreshToken
        });
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid userId) =>
        await _userRepository.GetActiveSubscriptionAsync(userId);

    public async Task<(bool Success, string? Error, object? Data)> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise)
    {
        var subscriptionId = await _userRepository.CreateSubscriptionAsync(userId, plan, amountPaise);
        return (true, null, new { SubscriptionId = subscriptionId, Status = "pending" });
    }

    public Task HandleRazorpayWebhookAsync(string eventType, string razorpaySubId, string status) =>
        _userRepository.HandleRazorpayWebhookAsync(eventType, razorpaySubId, status);

    public async Task<(bool Success, string Message, object? Data)> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        try
        {
        var user = await _userRepository.GetActiveUserByEmailAsync(request.Email);
        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);
        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshToken, DateTime.UtcNow.AddDays(refreshExpiryDays), ipAddress);

        return (true, "Login successful", new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = user,
            ExpiresIn = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60) * 60
        });
            
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Login  failed for {email}", request.Email);
            return (false, "Invalid login details", null);
        }


        
    }
}
