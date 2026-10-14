
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
    public bool Passed { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = new();
}

public class AvailableTestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? ExamId { get; set; }
    public Guid? StandardId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? ExamName { get; set; }
    public string? StandardName { get; set; }
    public string? SubjectName { get; set; }
    public int TotalQuestions { get; set; }
    public int TimeLimit { get; set; }
    public int PassingScore { get; set; }
}

public class TestQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string? OptionA { get; set; }
    public string? OptionB { get; set; }
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    public int CorrectOption { get; set; }
    public string? Explanation { get; set; }
    public string? Difficulty { get; set; }
    public string? Topic { get; set; }
    public string? Concept { get; set; }
    public string? ImageUrl { get; set; }
}

public class TestPayloadDto
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = "";
    public int TimeLimitSeconds { get; set; }        // NEW — total time
    public int PassingScore { get; set; }
    public List<TestQuestionDto> Questions { get; set; } = new();
}

public class SubmissionRequest
{
    public string? UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public int TimeTakenSeconds { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}




