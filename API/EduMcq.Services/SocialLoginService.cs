using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

public class SocialLoginService
{
    private readonly string _connectionString;
    private readonly IConfiguration _config;
    private readonly ILogger<SocialLoginService> _logger;
    private readonly JwtService _jwtService;

    public SocialLoginService(
        IConfiguration config,
        ILogger<SocialLoginService> logger,
        JwtService jwtService)
    {
        _connectionString = config["EduMcqDatabaseConnection"]!;
        _config = config;
        _logger = logger;
        _jwtService = jwtService;
    }

    public async Task<(bool Success, string Message, object? Data)> LoginAsync(SocialLoginRequest request, string? ipAddress = null)
    {
        SocialUserInfo? socialUser = null;

        try
        {
            socialUser = request.Provider.ToLower() switch
            {
                "google"   => await VerifyGoogleTokenAsync(request.IdToken),
                "facebook" => await VerifyFacebookTokenAsync(request.AccessToken ?? request.IdToken),
                "apple"    => await VerifyAppleTokenAsync(request.IdToken),
                _          => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Social token verification failed for {Provider}", request.Provider);
            return (false, "Invalid social token", null);
        }

        if (socialUser == null)
            return (false, "Unsupported provider or invalid token", null);

        // Find or Create user
        var user = await FindOrCreateUserAsync(request.Provider, socialUser);

        // Generate tokens
        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);

        // Save refresh token
        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        await SaveRefreshTokenAsync(user.UserId, refreshToken, 
            DateTime.UtcNow.AddDays(refreshExpiryDays), ipAddress);


        AuthResponse authResponse = new AuthResponse()
        {
            AccessToken= accessToken,
            RefreshToken = refreshToken,
            User=user,
 ExpiresIn = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60) * 60
        };
        return (true,"Login successful",authResponse);

        // return (true, "Login successful", new
        // {
        //     accessToken,
        //     refreshToken,
        //     expiresIn = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60) * 60,
        //     user = new
        //     {
        //         id = user.UserId,
        //         name = user.Name,
        //         email = user.Email,
        //         mobile = user.MobileNumber,
        //         role = user.Role,
        //         avatar = user.AvatarUrl
        //     }
        // });
    }

    // ==================== GOOGLE ====================
    private async Task<SocialUserInfo> VerifyGoogleTokenAsync(string idToken)
    {
        var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _config["SocialLogin:Google:ClientId"] }
        };

        var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);

        return new SocialUserInfo
        {
            ProviderId = payload.Subject,
            Email = payload.Email,
            Name = payload.Name,
            Picture = payload.Picture,
            EmailVerified = payload.EmailVerified
        };
    }

    // ==================== FACEBOOK ====================
    private async Task<SocialUserInfo> VerifyFacebookTokenAsync(string accessToken)
    {
        using var http = new HttpClient();
        var url = $"https://graph.facebook.com/me?fields=id,name,email,picture.type(large)&access_token={accessToken}";

        var response = await http.GetStringAsync(url);
        using var doc = System.Text.Json.JsonDocument.Parse(response);
        var root = doc.RootElement;

        return new SocialUserInfo
        {
            ProviderId = root.GetProperty("id").GetString()!,
            Email = root.TryGetProperty("email", out var email) ? email.GetString() : null,
            Name = root.TryGetProperty("name", out var name) ? name.GetString() : null,
            Picture = root.TryGetProperty("picture", out var pic) 
                      && pic.TryGetProperty("data", out var data)
                      && data.TryGetProperty("url", out var urlProp)
                      ? urlProp.GetString() : null,
            EmailVerified = true   // Facebook usually verified
        };
    }

    // ==================== APPLE ====================
    private async Task<SocialUserInfo> VerifyAppleTokenAsync(string idToken)
    {
        // For production you should properly validate the JWT signature using Apple's JWKS
        // This is a simplified version (you can improve later)

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                  ?? throw new Exception("Invalid Apple token");

        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        return new SocialUserInfo
        {
            ProviderId = sub,
            Email = email,
            Name = null,          // Apple only sends name the first time
            EmailVerified = true
        };
    }

    // ==================== FIND OR CREATE USER ====================
    private async Task<User> FindOrCreateUserAsync(string provider, SocialUserInfo social)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1. Try find by provider_id
        var column = provider.ToLower() switch
        {
            "google"   => "google_id",
            "facebook" => "facebook_id",
            "apple"    => "apple_id",
            _          => throw new ArgumentException("Invalid provider")
        };

        await using (var cmd = new NpgsqlCommand($@"
            SELECT id, phone, email, name, role, avatar_url
            FROM users 
            WHERE {column} = @providerId", conn))
        {
            cmd.Parameters.AddWithValue("providerId", social.ProviderId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
        }

        // 2. Try find by email (if available)
        if (!string.IsNullOrEmpty(social.Email))
        {
            await using var cmd = new NpgsqlCommand(@"
                SELECT id, phone, email, name, role, avatar_url
                FROM users 
                WHERE email = @email", conn);

            cmd.Parameters.AddWithValue("email", social.Email);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var existingUser = MapUser(reader);
                // Link the social ID
                await LinkSocialIdAsync(conn, existingUser.UserId, provider, social.ProviderId);
                return existingUser;
            }
        }

        // 3. Create new user
        var newUserId = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand($@"
            INSERT INTO users (id, email, name, {column}, avatar_url, is_email_verified, role, created_at, updated_at)
            VALUES (@id, @email, @name, @providerId, @avatar, @emailVerified, 'student', NOW(), NOW())", conn))
        {
            insert.Parameters.AddWithValue("id", newUserId);
            insert.Parameters.AddWithValue("email", (object?)social.Email ?? DBNull.Value);
            insert.Parameters.AddWithValue("name", (object?)social.Name ?? DBNull.Value);
            insert.Parameters.AddWithValue("providerId", social.ProviderId);
            insert.Parameters.AddWithValue("avatar", (object?)social.Picture ?? DBNull.Value);
            insert.Parameters.AddWithValue("emailVerified", social.EmailVerified);

            await insert.ExecuteNonQueryAsync();
        }

        // Also insert into social_logins table
        await using (var socialInsert = new NpgsqlCommand(@"
            INSERT INTO social_logins (user_id, provider, provider_id, provider_email)
            VALUES (@userId, @provider, @providerId, @email)", conn))
        {
            socialInsert.Parameters.AddWithValue("userId", newUserId);
            socialInsert.Parameters.AddWithValue("provider", provider.ToLower());
            socialInsert.Parameters.AddWithValue("providerId", social.ProviderId);
            socialInsert.Parameters.AddWithValue("email", (object?)social.Email ?? DBNull.Value);
            await socialInsert.ExecuteNonQueryAsync();
        }

        return new User
        {
            UserId = newUserId,
            Email = social.Email,
            Name = social.Name,
            Role = "student",
            Status = "active",
            AvatarUrl = social.Picture
        };
    }

    private async Task LinkSocialIdAsync(NpgsqlConnection conn, Guid userId, string provider, string providerId)
    {
        var column = provider.ToLower() switch
        {
            "google" => "google_id",
            "facebook" => "facebook_id",
            "apple" => "apple_id",
            _ => null
        };

        if (column == null) return;

        await using var cmd = new NpgsqlCommand($@"
            UPDATE users SET {column} = @providerId, updated_at = NOW()
            WHERE id = @userId AND {column} IS NULL", conn);

        cmd.Parameters.AddWithValue("providerId", providerId);
        cmd.Parameters.AddWithValue("userId", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    private User MapUser(NpgsqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetGuid(0),
            MobileNumber = reader.IsDBNull(1) ? null : reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            Name = reader.IsDBNull(3) ? null : reader.GetString(3),
            Role = reader.IsDBNull(4) ? "student" : reader.GetString(4),
            //Status = reader.IsDBNull(5) ? "active" : reader.GetString(5),
            AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    // Reuse the same SaveRefreshTokenAsync from OTP service
    private async Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAt, string? ip)
    {
        // copy the method you already have
    }
}