using System.Security.Claims;
using EduMcq.Models;
using EduMcq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



[ApiController]
[Route("api/subscriptions")]
//[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionsController(SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    // GET /api/subscriptions/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscription()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });

        var data = await _subscriptionService.GetMySubscriptionAsync(userId);
        return Ok(data);
    }

    // POST /api/subscriptions/create   (payment later)
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest req)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });

        var (success, error, data) = await _subscriptionService.CreateSubscriptionAsync(
            userId, req.Plan, req.AmountPaise);

        if (!success) return BadRequest(new { message = error });
        return Ok(data);
    }

    // POST /api/subscriptions/cancel
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Invalid user identity" });

        var (success, error) = await _subscriptionService.CancelSubscriptionAsync(userId);

        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Subscription cancelled" });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst("uid")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}