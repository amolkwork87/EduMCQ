using EduMcq.Interfaces;
using EduMcq.Models;
using Npgsql;

namespace EduMcq.Repository;

public class TestRepository : ITestRepository
{
    private readonly string _connectionString;

    public TestRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<List<ExamDto>> GetExamsAsync()
    {
        const string sql = "SELECT id, name, slug FROM exams WHERE is_active = true ORDER BY display_order, name";
        var exams = new List<ExamDto>();

        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            exams.Add(new ExamDto
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Slug = reader.GetString(2)
            });
        }

        return exams;
    }

    public async Task<List<StandardDto>> GetStandardsAsync(Guid examId)
    {
        const string sql = @"
            SELECT id, exam_id, name
            FROM standards
            WHERE exam_id = @examId AND is_active = true
            ORDER BY display_order, name";
        var standards = new List<StandardDto>();

        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("examId", examId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            standards.Add(new StandardDto
            {
                Id = reader.GetGuid(0),
                ExamId = reader.GetGuid(1),
                Name = reader.GetString(2)
            });
        }

        return standards;
    }

    public async Task<List<SubjectDto>> GetSubjectsAsync(Guid examId)
    {
        const string sql = @"
            SELECT id, exam_id, name
            FROM subjects
            WHERE exam_id = @examId AND is_active = true
            ORDER BY display_order, name";
        var subjects = new List<SubjectDto>();

        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("examId", examId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            subjects.Add(new SubjectDto
            {
                Id = reader.GetGuid(0),
                ExamId = reader.GetGuid(1),
                Name = reader.GetString(2)
            });
        }

        return subjects;
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync(Guid subjectId, Guid standardId, int limit)
    {
        const string sql = @"
            SELECT q.id, q.text, opt_a.text, opt_b.text, opt_c.text, opt_d.text,
                CASE
                    WHEN opt_a.is_correct THEN 0
                    WHEN opt_b.is_correct THEN 1
                    WHEN opt_c.is_correct THEN 2
                    WHEN opt_d.is_correct THEN 3
                    ELSE -1
                END,
                q.explanation,
                CASE
                    WHEN q.difficulty = 'easy' THEN 1
                    WHEN q.difficulty = 'medium' THEN 2
                    WHEN q.difficulty = 'hard' THEN 3
                    ELSE 1
                END,
                t.name, c.name
            FROM questions q
            LEFT JOIN options opt_a ON q.id = opt_a.question_id AND opt_a.label = 'A'
            LEFT JOIN options opt_b ON q.id = opt_b.question_id AND opt_b.label = 'B'
            LEFT JOIN options opt_c ON q.id = opt_c.question_id AND opt_c.label = 'C'
            LEFT JOIN options opt_d ON q.id = opt_d.question_id AND opt_d.label = 'D'
            LEFT JOIN topics t ON q.topic_id = t.id
            LEFT JOIN concepts c ON q.concept_id = c.id
            WHERE q.subject_id = @subjectId
              AND q.standard_id = @standardId
              AND q.status = 'published'
              AND q.text IS NOT NULL
            ORDER BY RANDOM()
            LIMIT @limit";

        var questions = new List<QuestionDto>();
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("subjectId", subjectId);
        command.Parameters.AddWithValue("standardId", standardId);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            questions.Add(ReadQuestion(reader));

        return questions;
    }

    public async Task<QuestionDto?> GetQuestionByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT q.id, q.text, opt_a.text, opt_b.text, opt_c.text, opt_d.text,
                CASE
                    WHEN opt_a.is_correct THEN 0
                    WHEN opt_b.is_correct THEN 1
                    WHEN opt_c.is_correct THEN 2
                    WHEN opt_d.is_correct THEN 3
                    ELSE -1
                END,
                q.explanation,
                CASE
                    WHEN q.difficulty = 'easy' THEN 1
                    WHEN q.difficulty = 'medium' THEN 2
                    WHEN q.difficulty = 'hard' THEN 3
                    ELSE 1
                END,
                t.name, c.name
            FROM questions q
            LEFT JOIN options opt_a ON q.id = opt_a.question_id AND opt_a.label = 'A'
            LEFT JOIN options opt_b ON q.id = opt_b.question_id AND opt_b.label = 'B'
            LEFT JOIN options opt_c ON q.id = opt_c.question_id AND opt_c.label = 'C'
            LEFT JOIN options opt_d ON q.id = opt_d.question_id AND opt_d.label = 'D'
            INNER JOIN topics t ON q.topic_id = t.id
            LEFT JOIN concepts c ON q.concept_id = c.id
            WHERE q.id = @id";

        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadQuestion(reader) : null;
    }

    public async Task SaveTestAttemptAsync(TestSubmissionDto submission, TestResultDto result)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        const string attemptSql = @"
            INSERT INTO test_attempts (id, user_id, subject_id, standard_id, total_questions,
                correct_count, wrong_count, unanswered_count, score_percentage,
                time_taken_seconds, started_at, completed_at)
            VALUES (gen_random_uuid(), @userId, @subjectId, @standardId, @total, @correct,
                @wrong, @unanswered, @score, @timeTaken, @startedAt, @completedAt)
            RETURNING id";

        await using var command = new NpgsqlCommand(attemptSql, connection, transaction);
        command.Parameters.AddWithValue("userId", submission.UserId);
        command.Parameters.AddWithValue("subjectId", submission.SubjectId);
        command.Parameters.AddWithValue("standardId", submission.StandardId);
        command.Parameters.AddWithValue("total", result.TotalQuestions);
        command.Parameters.AddWithValue("correct", result.CorrectCount);
        command.Parameters.AddWithValue("wrong", result.WrongCount);
        command.Parameters.AddWithValue("unanswered", result.UnansweredCount);
        command.Parameters.AddWithValue("score", result.ScorePercentage);
        command.Parameters.AddWithValue("timeTaken", result.TimeTakenSeconds);
        command.Parameters.AddWithValue("startedAt", submission.StartedAt);
        command.Parameters.AddWithValue("completedAt", DateTime.UtcNow);

        var attemptId = await command.ExecuteScalarAsync();
        foreach (var answer in submission.Answers)
        {
            var questionResult = result.QuestionResults.FirstOrDefault(r => r.QuestionId == answer.QuestionId);
            if (questionResult is null)
                continue;

            await using var answerCommand = new NpgsqlCommand(@"
                INSERT INTO test_answers (id, attempt_id, question_id, selected_option, is_correct)
                VALUES (gen_random_uuid(), @attemptId, @questionId, @selectedOption, @isCorrect)
                ON CONFLICT (attempt_id, question_id) DO UPDATE
                SET selected_option = EXCLUDED.selected_option, is_correct = EXCLUDED.is_correct", connection, transaction);
            answerCommand.Parameters.AddWithValue("attemptId", attemptId!);
            answerCommand.Parameters.AddWithValue("questionId", answer.QuestionId);
            answerCommand.Parameters.AddWithValue("selectedOption", answer.SelectedOption.HasValue ? (object)answer.SelectedOption.Value : DBNull.Value);
            answerCommand.Parameters.AddWithValue("isCorrect", questionResult.IsCorrect);
            await answerCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static QuestionDto ReadQuestion(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        QuestionText = reader.GetString(1),
        OptionA = reader.IsDBNull(2) ? null : reader.GetString(2),
        OptionB = reader.IsDBNull(3) ? null : reader.GetString(3),
        OptionC = reader.IsDBNull(4) ? null : reader.GetString(4),
        OptionD = reader.IsDBNull(5) ? null : reader.GetString(5),
        CorrectOption = reader.GetInt32(6),
        Explanation = reader.IsDBNull(7) ? null : reader.GetString(7),
        Difficulty = reader.GetInt32(8),
        Topic = reader.IsDBNull(9) ? null : reader.GetString(9),
        Concept = reader.IsDBNull(10) ? null : reader.GetString(10)
    };
}
