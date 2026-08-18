namespace EduMcq.Models;

public class OtpVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string OtpCode { get; set; } = null!;
    public string Purpose { get; set; } = "login"; // signup, password_reset, etc.
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
public class SendOtpRequest
{
    public string Phone { get; set; } = string.Empty;   // e.g. "9876543210" or "+919876543210"
    public string Purpose { get; set; } = "login";      // login | signup | password_reset
}

public class VerifyOtpRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string Purpose { get; set; } = "login";
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public User? User { get; set; }
    // Only in Development:
    public string? DebugOtp { get; set; }

    public int ExpiresIn {get;set;}
}

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword);

public record SignupRequest(
string FirstName,
string LastName,
string Email,
string Password,
string Role = "Student",
string? Phone = null,
string? Username = null,
string? ClassStandard = null,
string? SchoolName = null
);