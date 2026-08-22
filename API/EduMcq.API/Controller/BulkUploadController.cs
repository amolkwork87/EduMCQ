using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IImageImportService _imageImportService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IExcelImportService excelImportService,
        IImageImportService imageImportService,
        ILogger<UploadController> logger)
    {
        _excelImportService = excelImportService;
        _imageImportService = imageImportService;
        _logger = logger;
    }

    // --- LEGACY: Single-file upload with ZIP (kept for backward compatibility) ---
    [HttpPost("bulk-upload")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkUpload(IFormFile? zipFile, IFormFile excelFile, CancellationToken ct)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest(new { error = "Excel file is required." });

        if (excelFile.Length > 50 * 1024 * 1024)
            return BadRequest(new { error = "Excel file must be under 50 MB." });

        Dictionary<string, string>? imageMap = null;

        if (zipFile != null && zipFile.Length > 0)
        {
            var zipStream = zipFile.OpenReadStream();
            // imageMap = await _imageImportService.ProcessZipAsync(zipStream);
        }

        using var excelStream = excelFile.OpenReadStream();
        var result = await _excelImportService.ImportAsync(excelStream, imageMap);

        return Ok(new
        {
            message = $"Import complete: {result.SuccessCount} succeeded, {result.DuplicateCount} duplicates skipped, {result.FailedCount} failed.",
            total = result.TotalRows,
            success = result.SuccessCount,
            duplicates = result.DuplicateCount,
            failed = result.FailedCount,
            failedRows = result.FailedRows,
            duplicateRows = result.DuplicateRows
        });
    }

    // --- NEW: Batch upload from React frontend ---
    [HttpPost("bulk-upload-batch")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkUploadBatch([FromBody] BatchUploadRequest request, CancellationToken ct)
    {
        if (request?.Rows == null || request.Rows.Count == 0)
            return BadRequest(new { error = "No rows provided." });

        if (request.Rows.Count > 500)
            return BadRequest(new { error = "Batch size must not exceed 500 rows." });

        _logger.LogInformation(
            "Processing batch {BatchNumber}/{TotalBatches} with {RowCount} rows",
            request.BatchNumber, request.TotalBatches, request.Rows.Count);

        try
        {
            var createdById = GetCurrentUserId(); // Implement based on your auth
            var result = await _excelImportService.ImportBatchAsync(
                request.Rows,
                request.DuplicateHashes ?? new List<string>(),
                createdById);

            return Ok(new
            {
                success = result.SuccessCount > 0,
                successCount = result.SuccessCount,
                failureCount = result.FailureCount,
                duplicateCount = result.DuplicateCount,
                totalProcessed = request.Rows.Count,
                newHashes = result.NewHashes,
                errors = result.Errors,
                duplicates = result.Duplicates,
                status = result.Status,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch upload failed for batch {BatchNumber}", request.BatchNumber);
            return StatusCode(500, new
            {
                error = "Batch processing failed",
                details = ex.Message,
                batchNumber = request.BatchNumber
            });
        }
    }

    // --- Image upload (for individual image uploads from React) ---
    [HttpPost("upload-image")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage([FromForm] List<IFormFile> files, [FromForm] bool isQuestionImage = true)
    {
        
         if (files == null || files.Count == 0)
        return BadRequest(new { error = "No files received." });

    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
    var urls = new List<string>();
    var fileNames = new List<string>();

    foreach (var file in files)
    {
        if (file.Length == 0)
            continue;

        // Size check – 5 MB per file
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = $"{file.FileName} must be under 5 MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { error = $"{file.FileName}: only PNG, JPG, WebP, SVG allowed." });

        // ---- Option A: keep using your existing service (recommended) ----
        await using var stream = file.OpenReadStream();
        var url = await _imageImportService.UploadSingleAsync(
            stream,
            file.FileName,
            isQuestionImage ? "questions" : "answers");   // optional folder parameter


        urls.Add(url);
        fileNames.Add(file.FileName);
    }

    return Ok(new
    {
        urls,           // array of public paths
        fileNames       // original names (optional but useful for UI)
    });
    }

    // // --- Presigned URL for S3 upload (React uploads directly to S3) ---
    // [HttpPost("presign-image")]
    // [IgnoreAntiforgeryToken]
    // public IActionResult PresignImage([FromBody] PresignRequest request)
    // {
    //     // Implement with your S3/MinIO/R2 client
    //     // Returns { presignedUrl, key }
    //     var key = $"questions/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}-{request.FileName}";
    //     var presignedUrl = _imageImportService.GeneratePresignedUrl(key, request.ContentType);

    //     return Ok(new
    //     {
    //         presignedUrl,
    //         key,
    //         expiresIn = 300 // 5 minutes
    //     });
    // }

    private Guid? GetCurrentUserId()
    {
        // Implement based on your auth system (JWT, session, etc.)
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class PresignRequest
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public int? QuestionIndex { get; set; }
}