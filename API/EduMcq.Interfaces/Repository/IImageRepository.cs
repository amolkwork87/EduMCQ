namespace EduMcq.Interfaces;

public interface IImageRepository
{
    Task InsertMetadataAsync(string publicUrl, string originalFileName, string storedFileName,
        string contentType, long fileSizeBytes, bool isQuestionImage);
}