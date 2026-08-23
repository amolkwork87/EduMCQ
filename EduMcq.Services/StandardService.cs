using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class StandardService : IStandardService
{
    private readonly IStandardRepository _repository;
    public StandardService(IStandardRepository repository) => _repository = repository;
    public Task<Standard> CreateStandard(Guid examId, CreateStandardRequest req) => _repository.CreateStandard(examId, req);
    public Task<bool> DeleteStandard(Guid id) => _repository.DeleteStandard(id);
    public Task<List<Standard>> GetStandardsAsync(Guid examId, bool activeOnly = true) => _repository.GetStandardsAsync(examId, activeOnly);
    public Task<Standard?> UpdateStandard(Guid id, UpdateStandardRequest req) => _repository.UpdateStandard(id, req);
}
