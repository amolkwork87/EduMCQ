namespace EduMcq.Models;

public class QuestionDto
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; } = "";
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public int CorrectOption { get; set; }  // 0=A, 1=B, 2=C, 3=D
        public string? Explanation { get; set; }
        public int Difficulty { get; set; }
        public string? Topic { get; set; }
        public string? Concept { get; set; }
        public string? ThinkingSkills { get; set; }
    }
public class QuestionResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string? Topic { get; set; }
        public string? Concept { get; set; }
        public int Difficulty { get; set; }
        public string?[] Options { get; set; } = Array.Empty<string?>();
        public int CorrectOption { get; set; }
        public int? SelectedOption { get; set; }
        public string? Explanation { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
    }