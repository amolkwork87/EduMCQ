// Controllers/LeaderboardController.cs
using System.Security.Claims;
using EduMcq.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;

        if (claim == null || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("User id not found in token.");

        return userId;
    }

    /// <summary>All-time leaderboard</summary>
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobal(
        [FromQuery] string exam = "all",
        [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        var data = await _leaderboardService.GetGlobalAsync(userId, exam, limit);
        return Ok(data);
    }

    /// <summary>This-week leaderboard</summary>
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly(
        [FromQuery] string exam = "all",
        [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();
        var data = await _leaderboardService.GetWeeklyAsync(userId, exam, limit);
        return Ok(data);
    }

    /// <summary>Current user's own stats + global rank</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyStats()
    {
        var userId = GetCurrentUserId();
        var stats = await _leaderboardService.GetMyStatsAsync(userId);
        return Ok(stats);
    }
}