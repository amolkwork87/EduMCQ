// Models/MockTestTemplate.cs
namespace EduMcq.Models;
public class MockTestTemplate
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
    public int DifficultyEasyPct { get; set; }
    public int DifficultyMediumPct { get; set; }
    public int DifficultyHardPct { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public decimal AverageScore { get; set; }
    public List<Guid> QuestionIds { get; set; } = new();
}

// DTOs/MockTestTemplateRequest.cs
public class MockTestTemplateRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid? ExamId { get; set; }
    public Guid? StandardId { get; set; }
    public Guid? SubjectId { get; set; }
    public int TotalQuestions { get; set; }
    public int TimeLimit { get; set; }
    public int PassingScore { get; set; }
    public int DifficultyEasyPct { get; set; } = 30;
    public int DifficultyMediumPct { get; set; } = 50;
    public int DifficultyHardPct { get; set; } = 20;
    public bool IsActive { get; set; } = true;
    public List<Guid> QuestionIds { get; set; } = new();
}

// DTOs/QuestionFilterRequest.cs
public class QuestionFilterRequest
{
    public Guid? ExamId { get; set; }
    public Guid? StandardId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Difficulty { get; set; }
    public string? Search { get; set; }
    //public Guid? ExcludeTemplateId { get; set; } // when editing, exclude currently selected
}

// DTOs/QuestionListItem.cs
public class QuestionListItem
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Difficulty { get; set; }
    public string? Status { get; set; }
    public string? TopicName { get; set; }
    public string? ConceptName { get; set; }
    public int UsageCount { get; set; }        // how many OTHER tests use it
    public List<string> UsedInTests { get; set; } = new(); // test names for highlight
}