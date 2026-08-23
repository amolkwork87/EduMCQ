
using EduMcq.Models;

namespace EduMcq.Interfaces;
public interface IExamService
{
    Task<List<Exam>> GetExamsAsync(bool activeOnly = true);

    Task<Exam?> GetExamByIdAsync(Guid id);

    Task<Exam> CreateExam(CreateExamRequest req);

    Task<Exam?> UpdateExam(Guid id, UpdateExamRequest req);


    Task<bool> DeleteExam(Guid id);

}