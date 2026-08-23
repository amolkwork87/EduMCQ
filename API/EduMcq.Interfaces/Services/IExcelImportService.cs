
using EduMcq.Models.BulkUpload;
namespace EduMcq.Interfaces;

public interface IExcelImportService
{
    /// <summary>
    /// Legacy single-file import (ZIP + Excel)
    /// </summary>
    Task<BulkUploadResult> ImportAsync(
        Stream excelStream,
        Dictionary<string, string>? imageMap = null,
        Guid? createdById = null);

    /// <summary>
    /// Batch import from React frontend (500 rows max per batch)
    /// </summary>
    Task<BatchUploadResponse> ImportBatchAsync(
        List<QuestionRow> rows,
        List<string> duplicateHashes,
        Guid? createdById = null);
}