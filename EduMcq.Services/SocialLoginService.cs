using System.Text.Json;
using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EduMcq.Services;

public class SocialLoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;
    private readonly ILogger<SocialLoginService> _logger;
    private readonly JwtService _jwtService;

    public SocialLoginService(IUserRepository userRepository, IConfiguration config, ILogger<SocialLoginService> logger, JwtService jwtService)
    {
        _userRepository = userRepository;
        _config = config;
        _logger = logger;
        _jwtService = jwtService;
    }

    public async Task<(bool Success, string Message, object? Data)> LoginAsync(SocialLoginRequest request, string? ipAddress = null)
    {
        SocialUserInfo? socialUser;
        try
        {
            socialUser = request.Provider.ToLowerInvariant() switch
            {
                "google" => await VerifyGoogleTokenAsync(request.IdToken),
                "facebook" => await VerifyFacebookTokenAsync(request.AccessToken ?? request.IdToken),
                "apple" => await VerifyAppleTokenAsync(request.IdToken),
                _ => null
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Social token verification failed for {Provider}", request.Provider);
            return (false, "Invalid social token", null);
        }

        if (socialUser == null) return (false, "Unsupported provider or invalid token", null);

        var user = await _userRepository.FindOrCreateSocialUserAsync(request.Provider, socialUser);
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

    private async Task<SocialUserInfo> VerifyGoogleTokenAsync(string idToken)
    {
        var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _config["SocialLogin:Google:ClientId"] }
        };
        var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new SocialUserInfo { ProviderId = payload.Subject, Email = payload.Email, Name = payload.Name, Picture = payload.Picture, EmailVerified = payload.EmailVerified };
    }

    private async Task<SocialUserInfo> VerifyFacebookTokenAsync(string accessToken)
    {
        using var httpClient = new HttpClient();
        var url = $"https://graph.facebook.com/me?fields=id,name,email,picture.type(large)&access_token={accessToken}";
        using var document = JsonDocument.Parse(await httpClient.GetStringAsync(url));
        var root = document.RootElement;
        var picture = root.TryGetProperty("picture", out var pictureElement) && pictureElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
        return new SocialUserInfo
        {
            ProviderId = root.GetProperty("id").GetString()!,
            Email = root.TryGetProperty("email", out var email) ? email.GetString() : null,
            Name = root.TryGetProperty("name", out var name) ? name.GetString() : null,
            Picture = picture,
            EmailVerified = true
        };
    }

    private Task<SocialUserInfo> VerifyAppleTokenAsync(string idToken)
    {
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(idToken);
        var providerId = jwt.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value
            ?? throw new InvalidOperationException("Invalid Apple token");
        return Task.FromResult(new SocialUserInfo
        {
            ProviderId = providerId,
            Email = jwt.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value,
            EmailVerified = true
        });
    }
}
