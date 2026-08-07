using EduMcq.Interfaces;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/admin/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IImageImportService _imageImportService;
    public UploadController(IExcelImportService excelImportService,
    IImageImportService imageImportService)
    {
        _excelImportService = excelImportService;
        _imageImportService = imageImportService;
    }

    [HttpPost("bulk-upload")]
    [IgnoreAntiforgeryToken]
    
    public async Task<IActionResult> BulkUpload(IFormFile? zipFile, IFormFile excelFile, CancellationToken ct)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest(new { error = "Excel file is required." });

        if (excelFile.Length > 50 * 1024 * 1024)
            return BadRequest(new { error = "Excel file must be under 50 MB." });

        Dictionary<string, string>? imageMap = null;

        // Step 1: If ZIP uploaded, extract and upload images first
        if (zipFile != null && zipFile.Length > 0)
        {
            var zipStream = zipFile.OpenReadStream();
            //imageMap = await imageService.ProcessZipAsync(zipStream);
        }

        // Step 2: Parse Excel and bulk-insert
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


    [HttpPost("upload-image")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "Image must be under 2 MB." });

        var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { error = "Only PNG, JPG, WebP, SVG allowed." });

        using var stream = file.OpenReadStream();
        var url = await _imageImportService.UploadSingleAsync(stream, file.FileName);
        return Ok(new { url });
    }

}