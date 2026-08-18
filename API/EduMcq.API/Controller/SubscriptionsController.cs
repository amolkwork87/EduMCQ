using EduMcq.Models;
using EduMcq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly UserService _userService;

    public SubscriptionsController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var sub = await _userService.GetActiveSubscriptionAsync(userId);
        return Ok(sub);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest req)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var (success, error, data) = await _userService.CreateSubscriptionAsync(userId, req.Plan, req.AmountPaise);
        if (!success) return BadRequest(new { message = error });
        return Ok(data);
    }
}