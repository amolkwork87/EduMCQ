using EduMcq.Interfaces;

public class ImageImportService: IImageImportService
{
    public string GeneratePresignedUrl(string key, string contentType)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, string>> ProcessZipAsync(Stream zipStream)
    {
        throw new NotImplementedException();
    }

    public async Task<string> UploadSingleAsync(Stream stream, string fileName)
    {
        return string.Empty;
    }
}