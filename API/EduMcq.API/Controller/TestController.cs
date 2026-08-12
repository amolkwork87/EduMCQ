using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace EduMCQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<TestController> _logger;

        public TestController(IConfiguration configuration, ILogger<TestController> logger)
        {
            _connectionString = configuration["EduMcqDatabaseConnection"]!;
            _logger = logger;
        }

        // GET /api/test/exams
        [HttpGet("exams")]
        public async Task<IActionResult> GetExams()
        {
            var exams = new List<ExamDto>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT id, name, slug FROM exams WHERE is_active = true ORDER BY display_order, name", 
                connection);
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
            return Ok(exams);
        }

        // GET /api/test/standards?exam_id={guid}
        [HttpGet("standards")]
        public async Task<IActionResult> GetStandards([FromQuery] Guid exam_id)
        {
            var standards = new List<StandardDto>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT id, exam_id, name FROM standards WHERE exam_id = @examId AND is_active = true ORDER BY display_order, name", 
                connection);
            command.Parameters.AddWithValue("examId", exam_id);
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
            return Ok(standards);
        }

        // GET /api/test/subjects?exam_id={guid}
        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects([FromQuery] Guid exam_id)
        {
            var subjects = new List<SubjectDto>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT id, exam_id, name FROM subjects WHERE exam_id = @examId AND is_active = true ORDER BY display_order, name", 
                connection);
            command.Parameters.AddWithValue("examId", exam_id);
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
            return Ok(subjects);
        }

        // GET /api/test/questions?subject_id={guid}&standard_id={guid}&limit=10
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions(
            [FromQuery] Guid subject_id, 
            [FromQuery] Guid standard_id, 
            [FromQuery] int limit = 10)
        {
            var questions = new List<QuestionDto>();
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // KEY QUERY: Uses topic_standards junction table
            await using var command = new NpgsqlCommand(@"
                SELECT 
                    q.id,
                    q.question_text,
                    q.option_a,
                    q.option_b,
                    q.option_c,
                    q.option_d,
                    q.correct_option,
                    q.explanation,
                    q.difficulty,
                    t.name as topic_name,
                    c.name as concept_name,
                    ts.thinking_skill_names
                FROM questions q
                INNER JOIN topics t ON q.topic_id = t.id
                INNER JOIN topic_standards ts_link ON t.id = ts_link.topic_id
                LEFT JOIN concepts c ON q.concept_id = c.id
                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(ts.name, ', ') as thinking_skill_names
                    FROM question_thinking_skills qts
                    INNER JOIN thinking_skills ts ON qts.thinking_skill_id = ts.id
                    WHERE qts.question_id = q.id
                ) ts ON true
                WHERE t.subject_id = @subjectId
                  AND ts_link.standard_id = @standardId
                  AND q.is_active = true
                ORDER BY RANDOM()
                LIMIT @limit", 
                connection);

            command.Parameters.AddWithValue("subjectId", subject_id);
            command.Parameters.AddWithValue("standardId", standard_id);
            command.Parameters.AddWithValue("limit", limit);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                questions.Add(new QuestionDto
                {
                    Id = reader.GetGuid(0),
                    QuestionText = reader.GetString(1),
                    OptionA = reader.IsDBNull(2) ? null : reader.GetString(2),
                    OptionB = reader.IsDBNull(3) ? null : reader.GetString(3),
                    OptionC = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OptionD = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CorrectOption = reader.GetInt32(6),  // 0=A, 1=B, 2=C, 3=D
                    Explanation = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Difficulty = reader.GetInt32(8),
                    Topic = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Concept = reader.IsDBNull(10) ? null : reader.GetString(10),
                    ThinkingSkills = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }

            return Ok(questions);
        }

        // POST /api/test/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTest([FromBody] TestSubmissionDto submission)
        {
            // Calculate results
            int correct = 0, wrong = 0, unanswered = 0;
            var questionResults = new List<QuestionResultDto>();

            foreach (var answer in submission.Answers)
            {
                var question = await GetQuestionById(answer.QuestionId);
                if (question == null) continue;

                bool isAnswered = answer.SelectedOption.HasValue;
                bool isCorrect = isAnswered && answer.SelectedOption.Value == question.CorrectOption;

                if (!isAnswered) unanswered++;
                else if (isCorrect) correct++;
                else wrong++;

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

            // Optionally: Save to test_attempts table
            await SaveTestAttempt(submission, result);

            return Ok(result);
        }

        private async Task<QuestionDto?> GetQuestionById(Guid id)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                SELECT q.id, q.question_text, q.option_a, q.option_b, q.option_c, q.option_d,
                       q.correct_option, q.explanation, q.difficulty, t.name, c.name
                FROM questions q
                INNER JOIN topics t ON q.topic_id = t.id
                LEFT JOIN concepts c ON q.concept_id = c.id
                WHERE q.id = @id", connection);
            command.Parameters.AddWithValue("id", id);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new QuestionDto
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
            return null;
        }

        private async Task SaveTestAttempt(TestSubmissionDto submission, TestResultDto result)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                INSERT INTO test_attempts (id, user_id, subject_id, standard_id, total_questions, 
                    correct_count, wrong_count, unanswered_count, score_percentage, 
                    time_taken_seconds, started_at, completed_at)
                VALUES (gen_random_uuid(), @userId, @subjectId, @standardId, @total, @correct, 
                    @wrong, @unanswered, @score, @timeTaken, @startedAt, @completedAt)
                RETURNING id", connection);

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

            // Save individual answers
            foreach (var answer in submission.Answers)
            {
                await using var ansCmd = new NpgsqlCommand(@"
                    INSERT INTO test_answers (id, attempt_id, question_id, selected_option, is_correct)
                    VALUES (gen_random_uuid(), @attemptId, @questionId, @selectedOption, @isCorrect)
                    ON CONFLICT (attempt_id, question_id) DO UPDATE 
                    SET selected_option = EXCLUDED.selected_option, is_correct = EXCLUDED.is_correct",
                    connection);
                ansCmd.Parameters.AddWithValue("attemptId", attemptId!);
                ansCmd.Parameters.AddWithValue("questionId", answer.QuestionId);
                ansCmd.Parameters.AddWithValue("selectedOption", 
                    answer.SelectedOption.HasValue ? (object)answer.SelectedOption.Value : DBNull.Value);
                ansCmd.Parameters.AddWithValue("isCorrect", 
                    answer.SelectedOption.HasValue && result.QuestionResults
                        .First(r => r.QuestionId == answer.QuestionId).IsCorrect);
                await ansCmd.ExecuteNonQueryAsync();
            }
        }
    }

    // ===================== DTOs =====================

    public class ExamDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Slug { get; set; } = ""; }
    public class StandardDto { public Guid Id { get; set; } public Guid ExamId { get; set; } public string Name { get; set; } = ""; }
    public class SubjectDto { public Guid Id { get; set; } public Guid ExamId { get; set; } public string Name { get; set; } = ""; }

    public class QuestionDto
    {
        public Guid Id { get; set; }
        public string QuestionText { get; set; } = "";
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public int CorrectOption { get; set; }  // 0=A, 1=B, 2=C, 3=D
        public string? Explanation { get; set; }
        public int Difficulty { get; set; }
        public string? Topic { get; set; }
        public string? Concept { get; set; }
        public string? ThinkingSkills { get; set; }
    }

    public class TestSubmissionDto
    {
        public Guid UserId { get; set; }
        public Guid SubjectId { get; set; }
        public Guid StandardId { get; set; }
        public DateTime StartedAt { get; set; }
        public int TimeTakenSeconds { get; set; }
        public List<AnswerDto> Answers { get; set; } = new();
    }

    public class AnswerDto
    {
        public Guid QuestionId { get; set; }
        public int? SelectedOption { get; set; }  // null = unanswered
    }

    public class TestResultDto
    {
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int UnansweredCount { get; set; }
        public int ScorePercentage { get; set; }
        public int TimeTakenSeconds { get; set; }
        public List<QuestionResultDto> QuestionResults { get; set; } = new();
    }

    public class QuestionResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string? Topic { get; set; }
        public string? Concept { get; set; }
        public int Difficulty { get; set; }
        public string?[] Options { get; set; } = Array.Empty<string?>();
        public int CorrectOption { get; set; }
        public int? SelectedOption { get; set; }
        public string? Explanation { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
    }
}