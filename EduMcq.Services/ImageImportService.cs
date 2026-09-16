using EduMcq.Interfaces;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

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
        var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        if (!allowed.Contains(ext))
            throw new ArgumentException("Only PNG, JPG, and WebP images are allowed.");

        if (!stream.CanRead)
            throw new ArgumentException("The uploaded image cannot be read.");

        MemoryStream? bufferedStream = null;
        if (!stream.CanSeek)
        {
            bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream);
            bufferedStream.Position = 0;
            stream = bufferedStream;
        }

        if (stream.CanSeek)
            stream.Position = 0;

        await ValidateImageAsync(stream, ext);
        if (stream.CanSeek)
            stream.Position = 0;

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

        bufferedStream?.Dispose();
        return publicUrl;
    }

    private static async Task ValidateImageAsync(Stream stream, string extension)
    {
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
        stream.Position = 0;

        var hasValidSignature = extension == ".png"
            ? bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            : extension is ".jpg" or ".jpeg"
                ? bytesRead >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff
                : bytesRead >= 12
                    && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                    && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        if (!hasValidSignature)
            throw new ArgumentException("The file content does not match its image type.");

        try
        {
            _ = await Image.DetectFormatAsync(stream)
                ?? throw new ArgumentException("The image format could not be detected.");
            stream.Position = 0;
            using var image = await Image.LoadAsync(stream);
        }
        catch (Exception exception) when (exception is not ArgumentException)
        {
            throw new ArgumentException("The uploaded file is not a valid decoded image.", exception);
        }
    }

    private static string GetContentType(string ext) => ext switch
    {
        ".png"  => "image/png",
        ".jpg"  or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
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