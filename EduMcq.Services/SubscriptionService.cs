using EduMcq.Interfaces;
using EduMcq.Models;

public class SubscriptionService
{
private readonly ISubscriptionRepository _repository;

public SubscriptionService(ISubscriptionRepository repository)
{
    _repository = repository;
}

public async Task<MySubscriptionResponse> GetMySubscriptionAsync(Guid userId)
{
    var result = new MySubscriptionResponse();
    result.Current = await _repository.GetActiveSubscriptionAsync(userId);
    result.Usage = await _repository.GetMonthlyUsageAsync(userId, result.Current.Plan);
    result.BillingHistory = await _repository.GetBillingHistoryAsync(userId);
    return result;
}

public async Task<(bool Success, string? Error, SubscriptionDto? Data)> CreateSubscriptionAsync(
    Guid userId, string plan, int amountPaise)
{
    return await _repository.CreateSubscriptionAsync(userId, plan, amountPaise);
}

public async Task<(bool Success, string? Error)> CancelSubscriptionAsync(Guid userId)
{
    return await _repository.CancelSubscriptionAsync(userId);
}

public async Task<(bool Allowed, string? Message)> CanUserTakeTestAsync(Guid userId)
{
    return await _repository.CanUserTakeTestAsync(userId);
}
}