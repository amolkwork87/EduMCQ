// Interfaces/ILeaderboardRepository.cs
using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ILeaderboardRepository
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetGlobalAsync(Guid currentUserId, string exam, int limit);
    Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyAsync(Guid currentUserId, string exam, int limit);
    Task<UserLeaderboardStatsDto> GetMyStatsAsync(Guid currentUserId);
}