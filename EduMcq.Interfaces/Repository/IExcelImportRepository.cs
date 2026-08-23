using EduMcq.Models.BulkUpload;

namespace EduMcq.Interfaces;

public interface IExcelImportRepository
{
    Task PersistBatchAsync(IReadOnlyList<PreparedQuestion> questions, Guid? createdById);
}