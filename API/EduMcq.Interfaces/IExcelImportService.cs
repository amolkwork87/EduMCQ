
using EduMcq.Models.BulkUpload;
namespace EduMcq.Interfaces;

public interface IExcelImportService
{
    Task<BulkUploadResult> ImportAsync(
        Stream excelStream,
        Dictionary<string, string>? imageMap = null,
        Guid? createdById = null);
}