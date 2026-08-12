using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ISubjectRepository
{
    Task<List<Subject>> GetSubjectsAsync(Guid examId, bool activeOnly = true);
    Task<Subject> CreateSubject(Guid examId, CreateSubjectRequest req);
    Task<Subject?> UpdateSubject(Guid id, UpdateSubjectRequest req);
    Task<bool> DeleteSubject(Guid id);
}