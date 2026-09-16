using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class TestService : ITestService
{
    private readonly ITestRepository _repository;

    public TestService(ITestRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ExamDto>> GetExamsAsync() => _repository.GetExamsAsync();

    public Task<List<StandardDto>> GetStandardsAsync(Guid examId) => _repository.GetStandardsAsync(examId);

    public Task<List<SubjectDto>> GetSubjectsAsync(Guid examId) => _repository.GetSubjectsAsync(examId);

    public Task<List<QuestionDto>> GetQuestionsAsync(Guid subjectId, Guid standardId, int limit) =>
        _repository.GetQuestionsAsync(subjectId, standardId, limit);

    public async Task<TestResultDto> SubmitTestAsync(TestSubmissionDto submission)
    {
        int correct = 0;
        int wrong = 0;
        int unanswered = 0;
        var questionResults = new List<QuestionResultDto>();

        foreach (var answer in submission.Answers)
        {
            var question = await _repository.GetQuestionByIdAsync(answer.QuestionId);
            if (question is null)
                continue;

            var isAnswered = answer.SelectedOption.HasValue;
            var isCorrect = isAnswered && answer.SelectedOption!.Value == question.CorrectOption;

            if (!isAnswered)
                unanswered++;
            else if (isCorrect)
                correct++;
            else
                wrong++;

            questionResults.Add(new QuestionResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                Topic = question.Topic,
                Concept = question.Concept,
                Difficulty = question.Difficulty,
                Options = new[] { question.OptionA, question.OptionB, question.OptionC, question.OptionD },
                CorrectOption = question.CorrectOption,
                SelectedOption = answer.SelectedOption,
                Explanation = question.Explanation,
                IsCorrect = isCorrect,
                IsAnswered = isAnswered
            });
        }

        var result = new TestResultDto
        {
            TotalQuestions = submission.Answers.Count,
            CorrectCount = correct,
            WrongCount = wrong,
            UnansweredCount = unanswered,
            ScorePercentage = submission.Answers.Count > 0
                ? (int)Math.Round((double)correct / submission.Answers.Count * 100)
                : 0,
            TimeTakenSeconds = submission.TimeTakenSeconds,
            QuestionResults = questionResults
        };

        await _repository.SaveTestAttemptAsync(submission, result);
        return result;
    }
}
