// Interfaces/ILeaderboardService.cs
using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetGlobalAsync(Guid currentUserId, string exam = "all", int limit = 50);
    Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyAsync(Guid currentUserId, string exam = "all", int limit = 50);
    Task<UserLeaderboardStatsDto> GetMyStatsAsync(Guid currentUserId);
}