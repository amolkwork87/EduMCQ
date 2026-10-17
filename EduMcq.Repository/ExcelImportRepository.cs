using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using MySqlConnector;
using System.Text;

namespace EduMcq.Repository;

public class ExcelImportRepository : IExcelImportRepository
{
    private readonly string _connectionString;

    public ExcelImportRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task PersistBatchAsync(IReadOnlyList<PreparedQuestion> questions, Guid? createdById)
    {
        if (questions.Count == 0) return;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            foreach (var batch in questions.Chunk(100))
            {
                var questionRows = batch.Select(question =>
                {
                    var row = question.Row;
                    return new object?[]
                    {
                        question.QuestionId, question.ExamId, question.StandardId, question.SubjectId,
                        question.TopicId, question.ConceptId, row.QuestionText, row.QuestionImageUrl,
                        string.IsNullOrWhiteSpace(row.OptionAText) && string.IsNullOrWhiteSpace(row.OptionBText)
                            && string.IsNullOrWhiteSpace(row.OptionCText) && string.IsNullOrWhiteSpace(row.OptionDText),
                        row.Difficulty switch { 1 => "easy", 2 => "medium", _ => "hard" },
                        row.Status?.ToLower() == "published" ? "published" : "draft",
                        row.Explanation, question.Hash, createdById, DateTime.UtcNow
                    };
                }).ToList();

                await ExecuteBatchInsertAsync(connection, transaction, "questions",
                    ["id", "exam_id", "standard_id", "subject_id", "topic_id", "concept_id", "text", "image_url",
                     "has_image_options", "difficulty", "status", "explanation", "content_hash", "created_by", "created_at"],
                    questionRows);

                var optionRows = new List<object?[]>(batch.Length * 4);
                foreach (var question in batch)
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
                        optionRows.Add([
                            Guid.NewGuid(), question.QuestionId, option.Item1, option.Item2, option.Item3,
                            option.Item1 == question.Row.CorrectOption, position
                        ]);
                    }
                }

                await ExecuteBatchInsertAsync(connection, transaction, "options",
                    ["id", "question_id", "label", "text", "image_url", "is_correct", "position"],
                    optionRows);
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task ExecuteBatchInsertAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string table,
        string[] columns,
        IReadOnlyList<object?[]> rows)
    {
        if (rows.Count == 0) return;

        var sql = new StringBuilder($"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ");
        await using var command = new MySqlCommand { Connection = connection, Transaction = transaction };
        var valueRows = new List<string>(rows.Count);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var parameterNames = new string[columns.Length];
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var parameterName = $"p{rowIndex}_{columnIndex}";
                parameterNames[columnIndex] = $"@{parameterName}";
                command.Parameters.AddWithValue(parameterName, rows[rowIndex][columnIndex] ?? DBNull.Value);
            }
            valueRows.Add($"({string.Join(", ", parameterNames)})");
        }

        sql.Append(string.Join(", ", valueRows));
        command.CommandText = sql.ToString();
        await command.ExecuteNonQueryAsync();
    }
}
