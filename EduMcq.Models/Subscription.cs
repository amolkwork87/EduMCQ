
namespace EduMcq.Models;

// Models/SubscriptionDtos.cs

public class SubscriptionDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Plan { get; set; } = "free";          // free | pro | elite
    public string Status { get; set; } = "active";
    public int? AmountPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool AutoRenew { get; set; }

    public string? RazorpaySubscriptionId { get; set; }
    public string? RazorpayPaymentId { get; set; }
}

public class UsageDto
{
    public int QuestionsUsed { get; set; }
    public int QuestionsAllowed { get; set; }           // -1 = unlimited
    public int QuizzesTakenThisMonth { get; set; }
}

public class BillingHistoryItemDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;  // "₹999"
    public string Status { get; set; } = string.Empty;
}

public class MySubscriptionResponse
{
    public SubscriptionDto Current { get; set; } = new();
    public UsageDto Usage { get; set; } = new();
    public List<BillingHistoryItemDto> BillingHistory { get; set; } = new();
}

public class CreateSubscriptionRequest
{
    public string Plan { get; set; } = "pro";           // pro | elite
    public int AmountPaise { get; set; }
}

