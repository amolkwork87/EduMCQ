namespace EduMcq.Models.BulkUpload;
public class QuestionRow
{
    public int SNo { get; set; }
    public string? Exam { get; set; }
    public string? Class { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? Subtopic { get; set; }
    public string? Concept { get; set; }
    public string? ThinkingSkills { get; set; }  // comma-separated
    public int Difficulty { get; set; }              // 1, 2, 3
    public string? QuestionText { get; set; }
    public string? QuestionImageUrl { get; set; }
    public string? AdditionalImageUrls { get; set; } // comma-separated
    public string? OptionAText { get; set; }
    public string? OptionAImageUrl { get; set; }
    public string? OptionBText { get; set; }
    public string? OptionBImageUrl { get; set; }
    public string? OptionCText { get; set; }
    public string? OptionCImageUrl { get; set; }
    public string? OptionDText { get; set; }
    public string? OptionDImageUrl { get; set; }
    public string? CorrectOption { get; set; }       // A, B, C, D
    public string? Explanation { get; set; }
    public string? Status { get; set; }               // Draft, Published
}