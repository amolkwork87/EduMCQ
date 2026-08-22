using EduMcq.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

public class ImageImportService: IImageImportService
{
    private readonly string _connectionString;
    private readonly string _rootPath;

    public ImageImportService(IConfiguration config)
    {
        _connectionString = config["EduMcqDatabaseConnection"]!;
        _rootPath=config["ImageUploadPath"]!;
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

        // 3. Insert metadata with Npgsql
        const string sql = @"
            INSERT INTO image_library (
                image_url,
                original_file_name,
                stored_file_name,
                content_type,
                file_size_bytes,
                is_question_image,
                is_active,
                created_at
            ) VALUES (
                @image_url,
                @original_file_name,
                @stored_file_name,
                @content_type,
                @file_size_bytes,
                @is_question_image,
                TRUE,
                NOW()
            )
            RETURNING id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("image_url", publicUrl);
        cmd.Parameters.AddWithValue("original_file_name", originalFileName);
        cmd.Parameters.AddWithValue("stored_file_name", storedFileName);
        cmd.Parameters.AddWithValue("content_type", contentType);
        cmd.Parameters.AddWithValue("file_size_bytes", fileInfo.Length);
        cmd.Parameters.AddWithValue("is_question_image", isQuestionImage);
        //cmd.Parameters.AddWithValue("uploaded_by", (object?)GetCurrentUser() ?? DBNull.Value);

        var id = await cmd.ExecuteScalarAsync();   // optional – you can ignore the returned id

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