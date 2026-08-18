
namespace EduMcq.Models;

// Models/Subscription.cs
public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Plan { get; set; } = "monthly";
    public string? RazorpaySubscriptionId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string Status { get; set; } = "active";
    public int? AmountPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public record CreateSubscriptionRequest(string Plan, int AmountPaise);