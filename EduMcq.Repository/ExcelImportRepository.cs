using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EduMcq.Repository;

public class ExcelImportRepository : IExcelImportRepository
{
    private readonly string _connectionString;

    public ExcelImportRepository(IConfiguration configuration)
    {
        _connectionString = configuration["EduMcqDatabaseConnection"]
            ?? throw new InvalidOperationException("EduMcqDatabaseConnection is not configured.");
    }

    public async Task PersistBatchAsync(IReadOnlyList<PreparedQuestion> questions, Guid? createdById)
    {
        if (questions.Count == 0) return;
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using (var writer = await connection.BeginBinaryImportAsync(@"COPY questions
                (id, exam_id, standard_id, subject_id, topic_id, concept_id, text, image_url,
                 has_image_options, difficulty, status, explanation, content_hash, created_by, created_at)
                FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var question in questions)
                {
                    var row = question.Row;
                    await writer.StartRowAsync();
                    await writer.WriteAsync(question.QuestionId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync(question.ExamId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync(question.StandardId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync(question.SubjectId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync((object?)question.TopicId ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync((object?)question.ConceptId ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync((object?)row.QuestionText ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync((object?)row.QuestionImageUrl ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(string.IsNullOrWhiteSpace(row.OptionAText) && string.IsNullOrWhiteSpace(row.OptionBText) && string.IsNullOrWhiteSpace(row.OptionCText) && string.IsNullOrWhiteSpace(row.OptionDText), NpgsqlTypes.NpgsqlDbType.Boolean);
                    await writer.WriteAsync(row.Difficulty switch { 1 => "easy", 2 => "medium", _ => "hard" }, NpgsqlTypes.NpgsqlDbType.Varchar);
                    await writer.WriteAsync(row.Status?.ToLower() == "published" ? "published" : "draft", NpgsqlTypes.NpgsqlDbType.Varchar);
                    await writer.WriteAsync((object?)row.Explanation ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                    await writer.WriteAsync(question.Hash, NpgsqlTypes.NpgsqlDbType.Varchar);
                    await writer.WriteAsync((object?)createdById ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
                    await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                }
                await writer.CompleteAsync();
            }

            await using (var writer = await connection.BeginBinaryImportAsync(@"COPY options
                (id, question_id, label, text, image_url, is_correct, position) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var question in questions)
                {
                    var options = new[]
                    {
                        ("A", question.Row.OptionAText, question.Row.OptionAImageUrl),
                        ("B", question.Row.OptionBText, question.Row.OptionBImageUrl),
                        ("C", question.Row.OptionCText, question.Row.OptionCImageUrl),
                        ("D", question.Row.OptionDText, question.Row.OptionDImageUrl)
                    };
                    for (var position = 0; position < options.Length; position++)
                    {
                        var option = options[position];
                        await writer.StartRowAsync();
                        await writer.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
                        await writer.WriteAsync(question.QuestionId, NpgsqlTypes.NpgsqlDbType.Uuid);
                        await writer.WriteAsync(option.Item1, NpgsqlTypes.NpgsqlDbType.Varchar);
                        await writer.WriteAsync((object?)option.Item2 ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                        await writer.WriteAsync((object?)option.Item3 ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                        await writer.WriteAsync(option.Item1 == question.Row.CorrectOption, NpgsqlTypes.NpgsqlDbType.Boolean);
                        await writer.WriteAsync(position, NpgsqlTypes.NpgsqlDbType.Smallint);
                    }
                }
                await writer.CompleteAsync();
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
