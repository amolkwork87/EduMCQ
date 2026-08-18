namespace EduMcq.Models;
public class SocialLoginRequest
{
    public string Provider { get; set; } = string.Empty; // google | facebook | apple
    public string IdToken { get; set; } = string.Empty;  // Google & Apple
    public string? AccessToken { get; set; }             // Facebook (optional)
}

public class SocialUserInfo
{
    public string ProviderId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
}