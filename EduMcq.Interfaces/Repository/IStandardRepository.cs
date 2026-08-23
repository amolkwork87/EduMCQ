using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface IStandardRepository
{
    Task<List<Standard>> GetStandardsAsync(Guid examId, bool activeOnly = true);
    Task<Standard> CreateStandard(Guid examId, CreateStandardRequest req);
    Task<Standard?> UpdateStandard(Guid id, UpdateStandardRequest req);
    Task<bool> DeleteStandard(Guid id);
}