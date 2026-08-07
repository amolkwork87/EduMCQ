namespace EduMcq.Interfaces;
public interface IImageImportService
{
    Task<string> UploadSingleAsync(Stream stream, string fileName);
}