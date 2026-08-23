using EduMcq.Interfaces;
using Microsoft.Extensions.Configuration;

public class ImageImportService: IImageImportService
{
    private readonly string _rootPath;
    private readonly IImageRepository _imageRepository;

    public ImageImportService(IConfiguration config, IImageRepository imageRepository)
    {
        _rootPath=config["ImageUploadPath"]!;
        _imageRepository = imageRepository;
    }

    /// <summary>
    /// Uploads a single image, stores it on disk and records metadata in image_library.
    /// </summary>
    public async Task<string> UploadSingleAsync(
        Stream stream,
        string originalFileName,
        string subFolder = "questions")          // "q" = question, "a" = answer option
    {
        // 1. Validate & prepare unique name
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg" };
        if (!allowed.Contains(ext))
            throw new ArgumentException("Only PNG, JPG, WebP, SVG allowed.");

        var storedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var relativeFolder = Path.Combine("images", subFolder);
        var physicalDir = Path.Combine(_rootPath ?? "wwwroot", relativeFolder);
        Directory.CreateDirectory(physicalDir);

        var physicalPath = Path.Combine(physicalDir, storedFileName);
        var publicUrl = $"/{relativeFolder.Replace('\\', '/')}/{storedFileName}";

        // 2. Save file to disk
        await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
        {
            await stream.CopyToAsync(fs);
        }

        var fileInfo = new FileInfo(physicalPath);
        var contentType = GetContentType(ext);
        var isQuestionImage = subFolder.Equals("q", StringComparison.OrdinalIgnoreCase);

        await _imageRepository.InsertMetadataAsync(
            publicUrl, originalFileName, storedFileName, contentType, fileInfo.Length, isQuestionImage);

        return publicUrl;
    }

    private static string GetContentType(string ext) => ext switch
    {
        ".png"  => "image/png",
        ".jpg"  or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".svg"  => "image/svg+xml",
        _       => "application/octet-stream"
    };


    public string GeneratePresignedUrl(string key, string contentType)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, string>> ProcessZipAsync(Stream zipStream)
    {
        throw new NotImplementedException();
    }
}