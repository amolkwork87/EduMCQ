namespace EduMcq.Models;

// Models/User.cs
public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    public string? AppleId { get; set; }
    public string? Name { get; set; }
    public string Role { get; set; } = "student";
    public string? AvatarUrl { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? ClassStandard { get; set; }
    public string? SchoolName { get; set; }
    public Guid? TargetExamId { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Status {get;set;}
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? SchoolName { get; set; }
    public string? ClassStandard { get; set; }
    public Guid? TargetExamId { get; set; }
    public string? AvatarUrl { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public object? Preferences { get; set; }   // JSON
}

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? SchoolName { get; set; }
    public string? ClassStandard { get; set; }
    public Guid? TargetExamId { get; set; }
    public object? Preferences { get; set; }
}



