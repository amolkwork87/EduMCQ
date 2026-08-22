namespace EduMcq.Interfaces;
public interface IImageImportService
{
    Task<string> UploadSingleAsync(Stream stream,
        string originalFileName,
        string subFolder);
    string GeneratePresignedUrl(string key, string contentType);
    Task<Dictionary<string, string>> ProcessZipAsync(Stream zipStream);
}