using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;

    public ExamService(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public Task<Exam> CreateExam(CreateExamRequest req) => _examRepository.CreateExam(req);
    public Task<bool> DeleteExam(Guid id) => _examRepository.DeleteExam(id);
    public Task<Exam?> GetExamByIdAsync(Guid id) => _examRepository.GetExamByIdAsync(id);
    public Task<List<Exam>> GetExamsAsync(bool activeOnly = true) => _examRepository.GetExamsAsync(activeOnly);
    public Task<Exam?> UpdateExam(Guid id, UpdateExamRequest req) => _examRepository.UpdateExam(id, req);

}
