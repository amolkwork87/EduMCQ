namespace EduMcq.Models.BulkUpload;

public class BulkUploadResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<FailedRow> FailedRows { get; set; } = new();
    public List<DuplicateRow> DuplicateRows { get; set; } = new();
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Partial
    public string? Message { get; set; }
    public int BatchNumber { get; set; }
    public int TotalBatches { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class BatchUploadRequest
{
    public int BatchNumber { get; set; }
    public int TotalBatches { get; set; }
    public List<QuestionRow> Rows { get; set; } = new();
    public string Format { get; set; } = "TEXT_TEXT"; // TEXT_TEXT, IMAGE_TEXT, TEXT_IMAGE
    public List<string> DuplicateHashes { get; set; } = new(); // Hashes to skip (from previous batches)
}

public class BatchUploadResponse
{
    public bool Success { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalProcessed { get; set; }
    public int DuplicateCount { get; set; }
    public List<FailedRow> Errors { get; set; } = new();
    public List<DuplicateRow> Duplicates { get; set; } = new();
    public List<string> NewHashes { get; set; } = new(); // Send back so client can track
    public string? Message { get; set; }
    public string Status { get; set; } = "Completed";
}

public class BulkJobStatus
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}