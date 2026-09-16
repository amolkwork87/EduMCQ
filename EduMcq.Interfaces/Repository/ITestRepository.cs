using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ITestRepository
{
    Task<List<ExamDto>> GetExamsAsync();
    Task<List<StandardDto>> GetStandardsAsync(Guid examId);
    Task<List<SubjectDto>> GetSubjectsAsync(Guid examId);
    Task<List<QuestionDto>> GetQuestionsAsync(Guid subjectId, Guid standardId, int limit);
    Task<QuestionDto?> GetQuestionByIdAsync(Guid id);
    Task SaveTestAttemptAsync(TestSubmissionDto submission, TestResultDto result);
}
