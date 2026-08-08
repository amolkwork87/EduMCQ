namespace EduMcq.Interfaces;
public interface IImageImportService
{
    Task<string> UploadSingleAsync(Stream stream, string fileName);
    string GeneratePresignedUrl(string key, string contentType);
    Task<Dictionary<string, string>> ProcessZipAsync(Stream zipStream);
}