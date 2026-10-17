// Repositories/MockTestRepository.cs
using MySqlConnector;
using EduMcq.Models;
using EduMcq.Interfaces;
using Microsoft.Extensions.Configuration;

namespace EduMcq.Repository;
public class MockTestRepository
{
    private readonly string _connectionString;

    public MockTestRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    // ---------- GET ALL TEMPLATES ----------
    public async Task<List<MockTestTemplate>> GetAllAsync()
    {
        var list = new List<MockTestTemplate>();
        const string sql = @"
            SELECT t.id, t.name, t.description,
                   t.exam_id, e.name AS exam_name,
                   t.standard_id, s.name AS standard_name,
                   t.subject_id, sb.name AS subject_name,
                   t.total_questions, t.time_limit_minutes, t.passing_score,
                   t.difficulty_easy_pct, t.difficulty_medium_pct, t.difficulty_hard_pct,
                   t.is_active, t.created_at, t.last_used_at, t.usage_count, t.average_score
            FROM mock_test_templates t
            LEFT JOIN exams e ON e.id = t.exam_id
            LEFT JOIN standards s ON s.id = t.standard_id
            LEFT JOIN subjects sb ON sb.id = t.subject_id
            ORDER BY t.created_at DESC;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapTemplate(reader));
        }
        return list;
    }

    // ---------- GET BY ID (with question ids) ----------
    public async Task<MockTestTemplate?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT t.id, t.name, t.description,
                   t.exam_id, e.name AS exam_name,
                   t.standard_id, s.name AS standard_name,
                   t.subject_id, sb.name AS subject_name,
                   t.total_questions, t.time_limit_minutes, t.passing_score,
                   t.difficulty_easy_pct, t.difficulty_medium_pct, t.difficulty_hard_pct,
                   t.is_active, t.created_at, t.last_used_at, t.usage_count, t.average_score
            FROM mock_test_templates t
            LEFT JOIN exams e ON e.id = t.exam_id
            LEFT JOIN standards s ON s.id = t.standard_id
            LEFT JOIN subjects sb ON sb.id = t.subject_id
            WHERE t.id = @id;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        MockTestTemplate? tpl;
        await using (var cmd = new MySqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            tpl = MapTemplate(r);
        }

        // Load question ids
        await using (var qcmd = new MySqlCommand(
            "SELECT question_id FROM mock_test_questions WHERE template_id = @id ORDER BY display_order", connection))
        {
            qcmd.Parameters.AddWithValue("id", id);
            await using var qr = await qcmd.ExecuteReaderAsync();
            while (await qr.ReadAsync())
                tpl.QuestionIds.Add(qr.GetGuid(0));
        }
        return tpl;
    }

    // ---------- CREATE ----------
    public async Task<Guid> CreateAsync(MockTestTemplateRequest req, Guid? userId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();

        try
        {
            var id = Guid.NewGuid();
            const string insertTpl = @"
                INSERT INTO mock_test_templates
                  (id, name, description, exam_id, standard_id, subject_id,
                   total_questions, time_limit_minutes, passing_score,
                   difficulty_easy_pct, difficulty_medium_pct, difficulty_hard_pct,
                   is_active)
                VALUES (@id, @name, @desc, @exam, @std, @sub,
                        @tq, @tl, @ps, @de, @dm, @dh, @active);";

            await using (var cmd = new MySqlCommand(insertTpl, connection, tx))
            {
                AddTemplateParams(cmd, id, req, userId);
                await cmd.ExecuteNonQueryAsync();
            }

            await InsertQuestionsAsync(connection, tx, id, req.QuestionIds);
            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ---------- UPDATE ----------
    public async Task<bool> UpdateAsync(Guid id, MockTestTemplateRequest req)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            const string updTpl = @"
                UPDATE mock_test_templates
                SET name=@name, description=@desc,
                    exam_id=@exam, standard_id=@std, subject_id=@sub,
                    total_questions=@tq, time_limit_minutes=@tl, passing_score=@ps,
                    difficulty_easy_pct=@de, difficulty_medium_pct=@dm,
                    difficulty_hard_pct=@dh, is_active=@active,
                    updated_at=now()
                WHERE id=@id;";

            await using (var cmd = new MySqlCommand(updTpl, connection, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                AddTemplateParams(cmd, id, req, null, includeId: false);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) { await tx.RollbackAsync(); return false; }
            }

            // Replace question set
            await using (var del = new MySqlCommand(
                "DELETE FROM mock_test_questions WHERE template_id = @id", connection, tx))
            {
                del.Parameters.AddWithValue("id", id);
                await del.ExecuteNonQueryAsync();
            }

            await InsertQuestionsAsync(connection, tx, id, req.QuestionIds);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ---------- DELETE ----------
    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(
            "DELETE FROM mock_test_templates WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ---------- TOGGLE ACTIVE ----------
    public async Task<bool> ToggleActiveAsync(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(@"
            UPDATE mock_test_templates
            SET is_active = NOT is_active, updated_at = now()
            WHERE id = @id", connection);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ---------- FILTER QUESTIONS (with usage highlight) ----------
    public async Task<List<QuestionListItem>> FilterQuestionsAsync(QuestionFilterRequest f)
    {
        var list = new List<QuestionListItem>();

        var sql = @"
            SELECT q.id, q.text, q.image_url, q.difficulty, q.status,
                   tp.name AS topic_name, c.name AS concept_name,
                   COALESCE(u.usage_count, 0) AS usage_count,
                   COALESCE(u.test_names, '') AS test_names
            FROM questions q
            LEFT JOIN topics tp ON tp.id = q.topic_id
            LEFT JOIN concepts c ON c.id = q.concept_id
            LEFT JOIN (
                SELECT mtq.question_id,
                       COUNT(DISTINCT mtq.template_id) AS usage_count,
                       GROUP_CONCAT(DISTINCT mt.name ORDER BY mt.name SEPARATOR ', ') AS test_names
                FROM mock_test_questions mtq
                JOIN mock_test_templates mt ON mt.id = mtq.template_id
                GROUP BY mtq.question_id
            ) u ON u.question_id = q.id
            WHERE q.status = 'published'
              AND (@exam   IS NULL OR q.exam_id = @exam)
              AND (@std    IS NULL OR q.standard_id = @std)
              --AND (@sub    IS NULL OR q.subject_id = @sub)
              --AND (@topic  IS NULL OR q.topic_id = @topic)
              --AND (@diff   IS NULL OR q.difficulty = @diff)
              -- MySQL text search filter can be added here if needed.
            ORDER BY usage_count ASC, q.created_at DESC
            LIMIT 500;";

       await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(sql, connection);

        cmd.Parameters.AddWithValue("exam",   (object?)f.ExamId     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("std",    (object?)f.StandardId ?? DBNull.Value);
        // cmd.Parameters.AddWithValue("sub",    (object?)f.SubjectId  ?? DBNull.Value);
        // cmd.Parameters.AddWithValue("topic",  (object?)f.TopicId    ?? DBNull.Value);
        // cmd.Parameters.AddWithValue("diff",   (object?)f.Difficulty ?? DBNull.Value);
        //cmd.Parameters.AddWithValue("search", (object?)f.Search     ?? DBNull.Value);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new QuestionListItem
            {
                Id = r.GetGuid(0),
                Text = r.IsDBNull(1) ? "" : r.GetString(1),
                ImageUrl = r.IsDBNull(2) ? null : r.GetString(2),
                Difficulty = r.IsDBNull(3) ? null : r.GetString(3),
                Status = r.IsDBNull(4) ? null : r.GetString(4),
                TopicName = r.IsDBNull(5) ? null : r.GetString(5),
                ConceptName = r.IsDBNull(6) ? null : r.GetString(6),
                UsageCount = r.GetInt32(7),
                UsedInTests = r.IsDBNull(8) || string.IsNullOrEmpty(r.GetString(8))
                    ? new List<string>()
                    : r.GetString(8).Split(", ").ToList()
            });
        }
        return list;
    }

    // ---------- Lookups ----------
    public async Task<List<object>> GetExamsAsync() => await GetLookupAsync("exams");
    public async Task<List<object>> GetStandardsAsync(Guid? examId)
        => await GetLookupAsync("standards", "exam_id", examId);
    public async Task<List<object>> GetSubjectsAsync() => await GetLookupAsync("subjects");
    public async Task<List<object>> GetTopicsAsync(Guid? subjectId)
        => await GetLookupAsync("topics", "subject_id", subjectId);

    private async Task<List<object>> GetLookupAsync(string table, string? filterCol = null, Guid? filterVal = null)
    {
        var result = new List<object>();
        var sql = $"SELECT id, name FROM {table} WHERE is_active = true";
        if (filterCol != null) sql += $" AND {filterCol} = @f";
        sql += " ORDER BY name";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        if (filterCol != null)
            cmd.Parameters.AddWithValue("f", (object?)filterVal ?? DBNull.Value);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result.Add(new { id = r.GetGuid(0), name = r.GetString(1) });
        return result;
    }

    // ---------- Helpers ----------
    private static MockTestTemplate MapTemplate(MySqlDataReader r) => new()
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
        DifficultyEasyPct = r.GetInt32(12),
        DifficultyMediumPct = r.GetInt32(13),
        DifficultyHardPct = r.GetInt32(14),
        IsActive = r.GetBoolean(15),
        CreatedAt = r.GetDateTime(16),
        LastUsed = r.IsDBNull(17) ? null : r.GetDateTime(17),
        UsageCount = r.GetInt32(18),
        AverageScore = r.GetDecimal(19),
    };

    private static void AddTemplateParams(MySqlCommand cmd, Guid id,
        MockTestTemplateRequest req, Guid? userId, bool includeId = true)
    {
        if (includeId) cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", req.Name);
        cmd.Parameters.AddWithValue("desc", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("exam", (object?)req.ExamId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("std", (object?)req.StandardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sub", (object?)req.SubjectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("tq", req.TotalQuestions);
        cmd.Parameters.AddWithValue("tl", req.TimeLimit);
        cmd.Parameters.AddWithValue("ps", req.PassingScore);
        cmd.Parameters.AddWithValue("de", req.DifficultyEasyPct);
        cmd.Parameters.AddWithValue("dm", req.DifficultyMediumPct);
        cmd.Parameters.AddWithValue("dh", req.DifficultyHardPct);
        cmd.Parameters.AddWithValue("active", req.IsActive);
        //if (userId.HasValue) cmd.Parameters.AddWithValue("uid", userId.Value);
    }

    private static async Task InsertQuestionsAsync(MySqlConnection conn,
        MySqlTransaction tx, Guid templateId, List<Guid> questionIds)
    {
        if (questionIds == null || questionIds.Count == 0) return;

        const string sql = @"
            INSERT INTO mock_test_questions (template_id, question_id, display_order)
            VALUES (@t, @q, @o);";

        for (int i = 0; i < questionIds.Count; i++)
        {
            await using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("t", templateId);
            cmd.Parameters.AddWithValue("q", questionIds[i]);
            cmd.Parameters.AddWithValue("o", i);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}