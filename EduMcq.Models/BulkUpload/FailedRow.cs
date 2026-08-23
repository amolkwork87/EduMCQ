namespace EduMcq.Models.BulkUpload;
public class FailedRow
{
    public int RowNumber { get; set; }
    public string QuestionText { get; set; } = "";
    public string Error { get; set; } = "";
}