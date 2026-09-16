// Models/Dtos/LeaderboardDtos.cs
namespace EduMcq.Models;

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }          // total points (SUM of correct_count)
    public int Quizzes { get; set; }
    public decimal Accuracy { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class UserLeaderboardStatsDto
{
    public int? GlobalRank { get; set; }
    public int TotalPoints { get; set; }
    public decimal Accuracy { get; set; }
    public int QuizzesTaken { get; set; }
}