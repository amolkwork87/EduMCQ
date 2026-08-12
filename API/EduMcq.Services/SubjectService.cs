using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repository;
    public SubjectService(ISubjectRepository repository) => _repository = repository;
    public Task<Subject> CreateSubject(Guid examId, CreateSubjectRequest req) => _repository.CreateSubject(examId, req);
    public Task<bool> DeleteSubject(Guid id) => _repository.DeleteSubject(id);
    public Task<List<Subject>> GetSubjectsAsync(Guid examId, bool activeOnly = true) => _repository.GetSubjectsAsync(examId, activeOnly);
    public Task<Subject?> UpdateSubject(Guid id, UpdateSubjectRequest req) => _repository.UpdateSubject(id, req);
}
