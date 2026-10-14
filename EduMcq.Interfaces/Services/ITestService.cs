using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface ITestService
{
    Task<List<ExamDto>> GetExamsAsync();
    Task<List<StandardDto>> GetStandardsAsync(Guid examId);
    Task<List<SubjectDto>> GetSubjectsAsync(Guid examId);
    Task<List<QuestionDto>> GetQuestionsAsync(Guid subjectId, Guid standardId, int limit);
    Task<TestResultDto> SubmitTestAsync(TestSubmissionDto submission);

    Task<TestResultDto> SubmitAndScoreAsync(Guid templateId, SubmissionRequest req);
    Task<TestPayloadDto> GetTemplateQuestionsAsync(Guid templateId);
    Task<List<AvailableTestDto>> GetAvailableTemplatesAsync(Guid? examId, Guid? standardId, Guid? subjectId);
}
