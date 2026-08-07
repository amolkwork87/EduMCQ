namespace EduMcq.Models.BulkUpload;
public class BulkUploadResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<FailedRow> FailedRows { get; set; } = new();
    public List<DuplicateRow> DuplicateRows { get; set; } = new();
}