using EduMcq.Interfaces;
using EduMcq.Models;
using MySqlConnector;

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
        await using var command = new MySqlCommand(sql, connection);
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
        await using var command = new MySqlCommand(sql, connection);
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
        await using var command = new MySqlCommand(sql, connection);
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
        await using var command = new MySqlCommand(sql, connection);
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
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadQuestion(reader) : null;
    }

    public async Task SaveTestAttemptAsync(TestSubmissionDto submission, TestResultDto result)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var attemptId = Guid.NewGuid();
        const string attemptSql = @"
            INSERT INTO test_attempts (id, user_id, subject_id, standard_id, total_questions,
                correct_count, wrong_count, unanswered_count, score_percentage,
                time_taken_seconds, started_at, completed_at)
            VALUES (@attemptId, @userId, @subjectId, @standardId, @total, @correct,
                @wrong, @unanswered, @score, @timeTaken, @startedAt, @completedAt)";

        await using var command = new MySqlCommand(attemptSql, connection, transaction);
        command.Parameters.AddWithValue("attemptId", attemptId);
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

        await command.ExecuteNonQueryAsync();
        foreach (var answer in submission.Answers)
        {
            var questionResult = result.QuestionResults.FirstOrDefault(r => r.QuestionId == answer.QuestionId);
            if (questionResult is null)
                continue;

            await using var answerCommand = new MySqlCommand(@"
                INSERT INTO test_answers (id, attempt_id, question_id, selected_option, is_correct)
                VALUES (@id, @attemptId, @questionId, @selectedOption, @isCorrect)
                ON DUPLICATE KEY UPDATE
                selected_option = VALUES(selected_option), is_correct = VALUES(is_correct)", connection, transaction);
            answerCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            answerCommand.Parameters.AddWithValue("attemptId", attemptId);
            answerCommand.Parameters.AddWithValue("questionId", answer.QuestionId);
            answerCommand.Parameters.AddWithValue("selectedOption", answer.SelectedOption.HasValue ? (object)answer.SelectedOption.Value : DBNull.Value);
            answerCommand.Parameters.AddWithValue("isCorrect", questionResult.IsCorrect);
            await answerCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<MySqlConnection> OpenConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static QuestionDto ReadQuestion(MySqlDataReader reader) => new()
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

    public async Task<List<AvailableTestDto>> GetAvailableTemplatesAsync(
        Guid? examId, Guid? standardId, Guid? subjectId)
    {
        examId     = examId     == Guid.Empty ? null : examId;
    standardId = standardId == Guid.Empty ? null : standardId;
    subjectId  = subjectId  == Guid.Empty ? null : subjectId;

        const string sql = @"
            SELECT t.id, t.name, t.description,
                   t.exam_id, e.name,
                   t.standard_id, s.name,
                   t.subject_id, sb.name,
                   t.total_questions, t.time_limit_minutes, t.passing_score
            FROM mock_test_templates t
            LEFT JOIN exams e ON e.id = t.exam_id
            LEFT JOIN standards s ON s.id = t.standard_id
            LEFT JOIN subjects sb ON sb.id = t.subject_id
            WHERE t.is_active = true
              AND (@exam IS NULL OR t.exam_id = @exam)
              AND (@std  IS NULL OR t.standard_id = @std)
              AND (@sub  IS NULL OR t.subject_id = @sub)
            ORDER BY t.created_at DESC;";

        var list = new List<AvailableTestDto>();
        await using var connection = await OpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        // Use Guid parameters so MySqlConnector serializes UUID columns consistently.
    cmd.Parameters.Add(new MySqlParameter("exam", MySqlDbType.Guid)
    {
        Value = (object?)examId ?? DBNull.Value
    });
    cmd.Parameters.Add(new MySqlParameter("std", MySqlDbType.Guid)
    {
        Value = (object?)standardId ?? DBNull.Value
    });
    cmd.Parameters.Add(new MySqlParameter("sub", MySqlDbType.Guid)
    {
        Value = (object?)subjectId ?? DBNull.Value
    });

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new AvailableTestDto
            {
                Id = r.GetGuid(0),
                Name = r.GetString(1),
                Description = r.IsDBNull(2) ? "" : r.GetString(2),
                ExamId = r.IsDBNull(3) ? null : r.GetGuid(3),
                ExamName = r.IsDBNull(4) ? null : r.GetString(4),
                StandardId = r.IsDBNull(5) ? null : r.GetGuid(5),
                StandardName = r.IsDBNull(6) ? null : r.GetString(6),
                SubjectId = r.IsDBNull(7) ? null : r.GetGuid(7),
                SubjectName = r.IsDBNull(8) ? null : r.GetString(8),
                TotalQuestions = r.GetInt32(9),
                TimeLimit = r.GetInt32(10),
                PassingScore = r.GetInt32(11),
            });
        }
        return list;
    }

    // ------------------ 2. Load template's questions (randomized) ------------------
    public async Task<TestPayloadDto?> GetTemplateQuestionsAsync(Guid templateId)
    {
        await using var connection = await OpenConnectionAsync();

        TestPayloadDto payload;

        // Header
        const string headerSql = @"
            SELECT id, name, time_limit_minutes, passing_score
            FROM mock_test_templates
            WHERE id = @id AND is_active = true;";

        await using (var hcmd = new MySqlCommand(headerSql, connection))
        {
            hcmd.Parameters.AddWithValue("id", templateId);
            await using var hr = await hcmd.ExecuteReaderAsync();
            if (!await hr.ReadAsync()) return null;
            payload = new TestPayloadDto
            {
                TemplateId = hr.GetGuid(0),
                Name = hr.GetString(1),
                TimeLimitSeconds = hr.GetInt32(2) * 60,
                PassingScore = hr.GetInt32(3),
            };
        }

        // Questions — ORDER BY random() to shuffle per session.
        // Note: options are stored elsewhere in your schema (question_options).
        // Assuming an options table: options(id, question_id, option_order/letter, text, is_correct).
        const string qSql = @"SELECT
    q.id,
    q.text,
    q.image_url,
    '' as difficulty,
    q.explanation,
    tp.name AS topic_name,
    c.name  AS concept_name,
    MAX(CASE WHEN o.label = 'A' THEN o.text END) AS opt_a,
    MAX(CASE WHEN o.label = 'B' THEN o.text END) AS opt_b,
    MAX(CASE WHEN o.label = 'C' THEN o.text END) AS opt_c,
    MAX(CASE WHEN o.label = 'D' THEN o.text END) AS opt_d,
    -- MAX(CASE WHEN o.label = 'A' THEN o.image_url END) AS opt_a_image,
    -- MAX(CASE WHEN o.label = 'B' THEN o.image_url END) AS opt_b_image,
    -- MAX(CASE WHEN o.label = 'C' THEN o.image_url END) AS opt_c_image,
    -- MAX(CASE WHEN o.label = 'D' THEN o.image_url END) AS opt_d_image,
    MAX(CASE WHEN o.is_correct THEN o.label END)     AS correct_option
FROM mock_test_questions mtq
JOIN questions q  ON q.id = mtq.question_id
LEFT JOIN topics   tp ON tp.id = q.topic_id
LEFT JOIN concepts c  ON c.id  = q.concept_id
LEFT JOIN options  o  ON o.question_id = q.id
WHERE mtq.template_id = @tid
GROUP BY
    q.id, q.text, q.image_url, q.explanation,
    tp.name, c.name
ORDER BY random();";

        await using (var qcmd = new MySqlCommand(qSql, connection))
        {
            qcmd.Parameters.AddWithValue("tid", templateId);
            await using var qr = await qcmd.ExecuteReaderAsync();
            while (await qr.ReadAsync())
            {
                var correctLabel = qr.IsDBNull(11) ? null : qr.GetString(11);
                payload.Questions.Add(new TestQuestionDto
                {
                    Id = qr.GetGuid(0),
                    QuestionText = qr.IsDBNull(1) ? "" : qr.GetString(1),
                    ImageUrl = qr.IsDBNull(2) ? null : qr.GetString(2),
                    Difficulty = qr.IsDBNull(3) ? null : qr.GetString(3),
                    Explanation = qr.IsDBNull(4) ? null : qr.GetString(4),
                    Topic = qr.IsDBNull(5) ? null : qr.GetString(5),
                    Concept = qr.IsDBNull(6) ? null : qr.GetString(6),
                    OptionA = qr.IsDBNull(7) ? null : qr.GetString(7),
                    OptionB = qr.IsDBNull(8) ? null : qr.GetString(8),
                    OptionC = qr.IsDBNull(9) ? null : qr.GetString(9),
                    OptionD = qr.IsDBNull(10) ? null : qr.GetString(10),
                    CorrectOption = correctLabel switch
                        {
                            "A" => 0,   // change to 1 if you use 1-based
                            "B" => 1,
                            "C" => 2,
                            "D" => 3,
                            _   => 0
                        }
                });
            }
        }

        return payload;
    }

    // ------------------ 3. Score submission ------------------
    public async Task<TestResultDto> SubmitAndScoreAsync(Guid templateId, SubmissionRequest req)
    {
        // Pull correct answers for the template's questions
        var correctMap = new Dictionary<Guid, int>();
        var questionMeta = new Dictionary<Guid, TestQuestionDto>();

                await using var connection = await OpenConnectionAsync();

        Guid? subjectId = null;
        Guid? standardId = null;
        await using (var templateCommand = new MySqlCommand(@"
            SELECT subject_id, standard_id
            FROM mock_test_templates
            WHERE id = @id;", connection))
        {
            templateCommand.Parameters.AddWithValue("id", templateId);
            await using var templateReader = await templateCommand.ExecuteReaderAsync();
            if (await templateReader.ReadAsync())
            {
                subjectId = templateReader.IsDBNull(0) ? null : templateReader.GetGuid(0);
                standardId = templateReader.IsDBNull(1) ? null : templateReader.GetGuid(1);
            }
        }


        const string sql = @"SELECT q.id,
       q.text,
       '' as difficulty,
       q.explanation,
       tp.name AS topic_name,
       c.name  AS concept_name,
       o.opt_a,
       o.opt_b,
       o.opt_c,
       o.opt_d,
       o.correct_option
FROM mock_test_questions mtq
JOIN questions q  ON q.id = mtq.question_id
LEFT JOIN topics   tp ON tp.id = q.topic_id
LEFT JOIN concepts c  ON c.id  = q.concept_id
LEFT JOIN LATERAL (
    SELECT
        MAX(CASE WHEN o.label = 'A' THEN o.text END)       AS opt_a,
        MAX(CASE WHEN o.label = 'B' THEN o.text END)       AS opt_b,
        MAX(CASE WHEN o.label = 'C' THEN o.text END)       AS opt_c,
        MAX(CASE WHEN o.label = 'D' THEN o.text END)       AS opt_d,
        MAX(CASE WHEN o.is_correct THEN o.label END)       AS correct_option
    FROM options o
    WHERE o.question_id = q.id
) o ON true
WHERE mtq.template_id = @tid;";

        await using (var cmd = new MySqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("tid", templateId);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var correctLabel = r.IsDBNull(10) ? null : r.GetString(10);
                var dto = new TestQuestionDto
                {
                    Id = r.GetGuid(0),
                    QuestionText = r.IsDBNull(1) ? "" : r.GetString(1),
                    Difficulty = r.IsDBNull(2) ? null : r.GetString(2),
                    Explanation = r.IsDBNull(3) ? null : r.GetString(3),
                    Topic = r.IsDBNull(4) ? null : r.GetString(4),
                    Concept = r.IsDBNull(5) ? null : r.GetString(5),
                    OptionA = r.IsDBNull(6) ? null : r.GetString(6),
                    OptionB = r.IsDBNull(7) ? null : r.GetString(7),
                    OptionC = r.IsDBNull(8) ? null : r.GetString(8),
                    OptionD = r.IsDBNull(9) ? null : r.GetString(9),
                    CorrectOption = correctLabel switch
                        {
                            "A" => 0,   // change to 1 if you use 1-based
                            "B" => 1,
                            "C" => 2,
                            "D" => 3,
                            _   => 0
                        }
                };
                correctMap[dto.Id] = dto.CorrectOption;
                questionMeta[dto.Id] = dto;
            }
        }

        // Build result
        int correct = 0, wrong = 0, unanswered = 0;
        var questionResults = new List<QuestionResultDto>();

        foreach (var answer in req.Answers)
        {
            if (!questionMeta.TryGetValue(answer.QuestionId, out var meta))
                continue; // question not in this template — skip

            bool isAnswered = answer.SelectedOption.HasValue;
            bool isCorrect = isAnswered && answer.SelectedOption!.Value == meta.CorrectOption;

            if (!isAnswered) unanswered++;
            else if (isCorrect) correct++;
            else wrong++;

            questionResults.Add(new QuestionResultDto
            {
                QuestionId = meta.Id,
                QuestionText = meta.QuestionText,
                Topic = meta.Topic,
                Concept = meta.Concept,
                Difficulty = meta.Difficulty,
                Options = new List<string>
                {
                    meta.OptionA ?? "", meta.OptionB ?? "",
                    meta.OptionC ?? "", meta.OptionD ?? ""
                },
                CorrectOption = meta.CorrectOption,
                SelectedOption = answer.SelectedOption,
                Explanation = meta.Explanation,
                IsCorrect = isCorrect,
                IsAnswered = isAnswered,
            });
        }

        int total = questionResults.Count;
        int pct = total > 0 ? (int)Math.Round(correct * 100.0 / total) : 0;

        // Track usage in template
        await using (var upd = new MySqlCommand(@"
            UPDATE mock_test_templates
            SET usage_count = usage_count + 1,
                last_used_at = now(),
                average_score = ((average_score * usage_count) + @pct) / (usage_count + 1)
            WHERE id = @id;", connection))
        {
            upd.Parameters.AddWithValue("id", templateId);
            upd.Parameters.AddWithValue("pct", pct);
            await upd.ExecuteNonQueryAsync();
        }

        var result = new TestResultDto
        {
            TotalQuestions = total,
            CorrectCount = correct,
            WrongCount = wrong,
            UnansweredCount = unanswered,
            ScorePercentage = pct,
            TimeTakenSeconds = req.TimeTakenSeconds,
            Passed = pct >= 60,
            QuestionResults = questionResults,
        };

        if (Guid.TryParse(req.UserId, out var userId) && subjectId.HasValue && standardId.HasValue)
        {
            await SaveTestAttemptAsync(new TestSubmissionDto
            {
                UserId = userId,
                SubjectId = subjectId.Value,
                StandardId = standardId.Value,
                StartedAt = req.StartedAt,
                TimeTakenSeconds = req.TimeTakenSeconds,
                Answers = req.Answers
            }, result);
        }

        return result;
    }
}
