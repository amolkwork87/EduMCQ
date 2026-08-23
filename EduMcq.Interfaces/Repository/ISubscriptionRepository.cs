using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ISubscriptionRepository
{
    Task<SubscriptionDto> GetActiveSubscriptionAsync(Guid userId);
    Task<UsageDto> GetMonthlyUsageAsync(Guid userId, string plan);
    Task<List<BillingHistoryItemDto>> GetBillingHistoryAsync(Guid userId);
    Task<(bool Success, string? Error, SubscriptionDto? Data)> CreateSubscriptionAsync(Guid userId, string plan, int amountPaise);
    Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid userId);
    Task<(bool Allowed, string? Message)> CanUserTakeTestAsync(Guid userId);
}
