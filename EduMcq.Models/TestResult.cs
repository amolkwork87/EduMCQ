
namespace EduMcq.Models;

public class TestSubmissionDto
{
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid StandardId { get; set; }
    public DateTime StartedAt { get; set; }
    public int TimeTakenSeconds { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid QuestionId { get; set; }
    public int? SelectedOption { get; set; }  // null = unanswered
}

public class TestResultDto
{
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int UnansweredCount { get; set; }
    public int ScorePercentage { get; set; }
    public int TimeTakenSeconds { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}