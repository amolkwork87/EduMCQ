using System.Security.Claims;
using EduMcq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/user/progress")]
[Authorize]   // only logged-in users
public class ProgressController : ControllerBase
{
    private readonly ProgressService _progressService;

    public ProgressController(ProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProgress([FromQuery] int period = 30)
    {
        // Get current user id from claims
        var userIdClaim = User.FindFirst("sub")?.Value 
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user" });

        var data = await _progressService.GetUserProgressAsync(userId, period);
        return Ok(data);
    }
}