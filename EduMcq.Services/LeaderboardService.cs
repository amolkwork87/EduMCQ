// Services/LeaderboardService.cs
using EduMcq.Interfaces;
using EduMcq.Models;
namespace EduMcq.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _repo;

    public LeaderboardService(ILeaderboardRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> GetGlobalAsync(Guid currentUserId, string exam = "all", int limit = 50)
        => _repo.GetGlobalAsync(currentUserId, exam, limit);

    public Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyAsync(Guid currentUserId, string exam = "all", int limit = 50)
        => _repo.GetWeeklyAsync(currentUserId, exam, limit);

    public Task<UserLeaderboardStatsDto> GetMyStatsAsync(Guid currentUserId)
        => _repo.GetMyStatsAsync(currentUserId);
}