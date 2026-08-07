namespace EduMcq.Models.BulkUpload;
public class DuplicateRow
{
    public int RowNumber { get; set; }
    public string QuestionText { get; set; } = "";
    public Guid ExistingQuestionId { get; set; }
}