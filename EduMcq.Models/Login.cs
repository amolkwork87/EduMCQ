namespace EduMcq.Models;
public class LoginRequest
{
    public string Email { get; set; } = string.Empty; // google | facebook | apple
    public string Password { get; set; } = string.Empty;  // Google & Apple
    public string? Phone { get; set; }             // Facebook (optional)

    public string? UserName { get; set; }             // Facebook (optional)

}

