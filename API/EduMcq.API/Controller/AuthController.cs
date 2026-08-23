using System.Security.Claims;
using EduMcq.Models;
using EduMcq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly OtpService _otpService;
    private readonly JwtService _jwtService;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly SocialLoginService _socialLoginService;
    private readonly UserService _userService;

    public AuthController(OtpService otpService, JwtService jwtService,
    IWebHostEnvironment env, IConfiguration config, SocialLoginService socialLoginService,
    UserService userService
    )
    {
        _otpService = otpService;
        _jwtService = jwtService;
        _env = env;
        _config = config;
        _socialLoginService = socialLoginService;
        _userService = userService;
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { message = "Phone number is required" });

        var (success, message, debugOtp) = await _otpService.SendOtpAsync(request.Phone, request.Purpose);

        if (!success)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message });

        var response = new { message, success = true };

        // Only expose OTP in Development
        if (_env.IsDevelopment() && debugOtp != null)
            return Ok(new { message, success = true, debugOtp });

        return Ok(response);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Otp))
            return BadRequest(new { success = false, message = "Phone and OTP are required" });

        var (success, message, user) = await _otpService.VerifyOtpAndGetUserAsync(
            request.Phone, request.Otp, request.Purpose ?? "login");

        if (!success || user == null)
            return Unauthorized(new { success = false, message });

        // Generate tokens
        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user);

        // Save refresh token
        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        await _otpService.SaveRefreshTokenAsync(
            user.UserId,
            refreshToken,
            DateTime.UtcNow.AddDays(refreshExpiryDays),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            success = true,
            message = "Login successful",
            accessToken,
            refreshToken,
            expiresIn = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60) * 60, // seconds
            user = new
            {
                id = user.UserId,
                name = user.Name,
                mobile = user.MobileNumber,
                role = user.Role,
                email = user.Email,
                status = user.Status
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { success = false, message = "Refresh token is required" });

        var (isValid, userId) = await _otpService.ValidateRefreshTokenAsync(request.RefreshToken);

        if (!isValid || userId == null)
            return Unauthorized(new { success = false, message = "Invalid or expired refresh token" });

        // Get user
        var user = await _otpService.GetUserByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized(new { success = false, message = "User not found" });

        // Optional: Token Rotation (recommended)
        // 1. Revoke old refresh token
        await _otpService.RevokeRefreshTokenAsync(request.RefreshToken);

        // 2. Generate new pair
        var (newAccessToken, newRefreshToken) = _jwtService.GenerateTokens(user);

        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays", 30);
        await _otpService.SaveRefreshTokenAsync(
            user.UserId,
            newRefreshToken,
            DateTime.UtcNow.AddDays(refreshExpiryDays),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            success = true,
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresIn = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60) * 60
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { success = false, message = "Refresh token is required" });

        await _otpService.RevokeRefreshTokenAsync(
            request.RefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new { success = true, message = "Logged out successfully" });
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst("uid")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _otpService.RevokeAllRefreshTokensAsync(userId);

        return Ok(new { success = true, message = "Logged out from all devices" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Email and Password are required" });

        var result = await _userService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Success)
            return Unauthorized(new { success = false, message = result.Message });

        //return Ok(new { success = true, message = result.Message, data = result.Data });
        return Ok(result.Data);
    }

    [HttpPost("social-login")]
    public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(new { success = false, message = "Provider and IdToken are required" });

        var result = await _socialLoginService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Success)
            return Unauthorized(new { success = false, message = result.Message });

        //return Ok(new { success = true, message = result.Message, data = result.Data });
        return Ok(result.Data);
    }

    /// POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Always return 200 to avoid email enumeration
        await _userService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    /// <summary>
    /// POST /api/auth/reset-password
    /// Matches ForgotPasswordPage → handleResetSubmit
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (req.NewPassword != req.ConfirmPassword)
            return BadRequest(new { message = "Passwords do not match" });
        if (req.NewPassword.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters" });

        var (success, error) = await _userService.ResetPasswordAsync(req.Token, req.NewPassword);
        if (!success) return Unauthorized(new { message = error });

        return Ok(new { message = "Password reset successful" });
    }

    /// <summary>
    /// POST /api/auth/signup
    /// Matches SignupPage → handleSubmit
    /// </summary>
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
       var (success, error, data) = await _userService.SignupAsync(request);
        if (!success) return BadRequest(new { message = error });
        return Ok(data);
    }
}