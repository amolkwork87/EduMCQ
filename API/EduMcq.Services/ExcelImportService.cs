using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace EduMCQ.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly string _connectionString;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        IConfiguration configuration,
        ILogger<ExcelImportService> logger)
    {
        _connectionString = configuration["EduMcqDatabaseConnection"]
            ?? throw new InvalidOperationException(
                "Configuration key 'EduMcqDatabaseConnection' is missing.");
        _logger = logger;
    }

    // --- Single-file import (legacy, for ZIP+Excel upload) ---
    public async Task<BulkUploadResult> ImportAsync(
        Stream excelStream,
        Dictionary<string, string>? imageMap = null,
        Guid? createdById = null)
    {
        var result = new BulkUploadResult();
        imageMap ??= new();

        var rows = ParseExcel(excelStream);
        result.TotalRows = rows.Count;

        using var db = new Infrastructure.AppDb(_connectionString);
        await using var tx = await db.Connection!.BeginTransactionAsync();

        // Preload all caches for fast lookups
        await db.PreloadCachesAsync(tx);

        try
        {
            foreach (var row in rows)
            {
                try
                {
                    await ImportRowFastAsync(db, tx, row, imageMap, createdById, result);
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedRows.Add(new FailedRow
                    {
                        RowNumber = row.SNo,
                        QuestionText = row.QuestionText ?? "",
                        Error = ex.Message
                    });
                }
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return result;
    }

    // --- BATCH IMPORT from React frontend ---
    public async Task<BatchUploadResponse> ImportBatchAsync(
        List<QuestionRow> rows,
        List<string> duplicateHashes,
        Guid? createdById = null)
    {
        var response = new BatchUploadResponse();
        var newHashes = new List<string>();

        using var db = new Infrastructure.AppDb(_connectionString);

        var conn = await db.GetConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Preload caches for this batch
        await db.PreloadCachesAsync(tx);

        try
        {
            // --- BULK INSERT: Questions ---
            var validRows = new List<(QuestionRow row, Guid questionId,
            string hash, Guid examId, Guid standardId, Guid subjectId, Guid? topicId, Guid? conceptId)>();

            foreach (var row in rows)
            {
                try
                {
                    var (questionId, hash, examId, standardId, subjectId, topicId, conceptId) =
                        await PrepareRowAsync(db, tx, row, duplicateHashes, newHashes, createdById);

                    if (questionId != Guid.Empty)
                    {
                        validRows.Add((row, questionId, hash, examId, standardId, subjectId, topicId, conceptId));
                    }
                    else
                    {
                        response.DuplicateCount++;
                        response.Duplicates.Add(new DuplicateRow
                        {
                            RowNumber = row.SNo,
                            QuestionText = row.QuestionText ?? "",
                            ExistingQuestionId = Guid.Empty
                        });
                    }
                }
                catch (Exception ex)
                {
                    response.FailureCount++;
                    response.Errors.Add(new FailedRow
                    {
                        RowNumber = row.SNo,
                        QuestionText = row.QuestionText ?? "",
                        Error = ex.Message
                    });
                }
            }

            // Bulk insert questions using COPY
            if (validRows.Count > 0)
            {
                await BulkInsertQuestionsAsync(db, tx, validRows, createdById);
                await BulkInsertOptionsAsync(db, tx, validRows);
                //await BulkInsertAssociationsAsync(db, tx, validRows);

                response.SuccessCount = validRows.Count;
                response.NewHashes = newHashes;
            }

            await tx.CommitAsync();
            response.Status = response.FailureCount > 0 ? "Partial" : "Completed";
            response.Message = $"Processed {rows.Count}: {response.SuccessCount} success, {response.DuplicateCount} duplicates, {response.FailureCount} failed";
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "Batch import failed");
            response.Success = false;
            response.Status = "Failed";
            response.Message = $"Batch failed: {ex.Message}";
            throw;
        }

        return response;
    }

    private async Task<(Guid questionId, string hash, Guid examId, Guid standardId, Guid subjectId, Guid? topicId, Guid? conceptId)>
        PrepareRowAsync(
            Infrastructure.AppDb db, NpgsqlTransaction tx,
            QuestionRow row, List<string> duplicateHashes, List<string> newHashes,
            Guid? createdById)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(row.QuestionText) && string.IsNullOrWhiteSpace(row.QuestionImageUrl))
            throw new Exception("Question text and image URL are both empty.");
        if (row.CorrectOption is not ("A" or "B" or "C" or "D"))
            throw new Exception($"Invalid Correct_Option: '{row.CorrectOption}'. Must be A, B, C, or D.");
        if (row.Difficulty is not (1 or 2 or 3))
            throw new Exception($"Invalid Difficulty: {row.Difficulty}. Must be 1, 2, or 3.");

        // Check options
        var hasAnyOption = !string.IsNullOrWhiteSpace(row.OptionAText) || !string.IsNullOrWhiteSpace(row.OptionAImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionBText) || !string.IsNullOrWhiteSpace(row.OptionBImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionCText) || !string.IsNullOrWhiteSpace(row.OptionCImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionDText) || !string.IsNullOrWhiteSpace(row.OptionDImageUrl);
        if (!hasAnyOption) throw new Exception("All 4 options are empty.");

        // Compute hash
        var optTexts = new[] { row.OptionAText ?? "", row.OptionBText ?? "", row.OptionCText ?? "", row.OptionDText ?? "" };
        var hash = ComputeHash(row.QuestionText ?? "", optTexts[0], optTexts[1], optTexts[2], optTexts[3]);

        // Check duplicates (from DB cache + client-provided hashes)
        if (db.IsDuplicateHash(hash) || duplicateHashes.Contains(hash))
        {
            return (Guid.Empty, hash, Guid.Empty, Guid.Empty, Guid.Empty, null, null);
        }

        // Resolve hierarchy using cached lookups
        var examId = db.GetExamId(row.Exam ?? "")
            ?? throw new Exception($"Exam not found: '{row.Exam}'");
        var standardId = db.GetStandardId(examId, row.Class ?? "") //row.class 
            ?? throw new Exception($"Class not found: '{row.Exam}' under exam '{row.Exam}'");
        var subjectId = db.GetSubjectId(examId, row.Subject ?? "") // row.subject
            ?? throw new Exception($"Subject not found: '{row.Exam}' under exam '{row.Exam}'");

        Guid? topicId = null;
        Guid? conceptId = null;

        if (!string.IsNullOrWhiteSpace(row.Topic))
        {
            // If Subtopic exists, try to find it as a child of Topic
            if (!string.IsNullOrWhiteSpace(row.Subtopic))
            {
                topicId = db.GetTopicId(subjectId, row.Topic, row.Subtopic);

                // If subtopic not found, try to find just the topic
                if (!topicId.HasValue)
                {
                    _logger.LogWarning($"Subtopic '{row.Subtopic}' not found under topic '{row.Topic}'. Trying to use topic as parent.");
                    topicId = db.GetTopicId(subjectId, row.Topic);
                }
            }
            else
            {
                // No subtopic, just find the topic
                topicId = db.GetTopicId(subjectId, row.Topic);
            }

            if (!topicId.HasValue)
            {
                throw new Exception($"Topic not found: '{row.Topic}'{(string.IsNullOrWhiteSpace(row.Subtopic) ? "" : $" / '{row.Subtopic}'")} under subject '{row.Subject}'");
            }

            // Now find concept under the topic
            if (!string.IsNullOrWhiteSpace(row.Concept))
            {
                conceptId = db.GetConceptId(topicId.Value, row.Concept)
                    ?? throw new Exception($"Concept not found: '{row.Concept}' under topic '{row.Topic}'");
            }
        }
        else if (!string.IsNullOrWhiteSpace(row.Concept))
        {
            throw new Exception($"Concept '{row.Concept}' provided but Topic is missing");
        }

        var questionId = Guid.NewGuid();
        newHashes.Add(hash);
        db.AddHash(hash);

        return (questionId, hash, examId, standardId, subjectId, topicId, conceptId);
    }

    // --- BULK INSERT using PostgreSQL COPY (fastest method) ---
    private async Task BulkInsertQuestionsAsync(
        Infrastructure.AppDb db, NpgsqlTransaction tx,
        List<(QuestionRow row, Guid questionId, string hash, Guid examId, Guid standardId, Guid subjectId, Guid? topicId, Guid? conceptId)> validRows,
        Guid? createdById)
    {
        var conn = db.Connection!;

        // Use COPY for bulk insert
        using var writer = await conn.BeginBinaryImportAsync(
            @"COPY questions (id, exam_id, standard_id, subject_id,topic_id,concept_id, text, image_url, 
                has_image_options, difficulty, status, explanation, content_hash, created_by, created_at) 
            FROM STDIN (FORMAT BINARY)");

        foreach (var (row, questionId, hash, examId, standardId, subjectId, topicId, conceptId) in validRows)
        {
            bool hasImageOptions = string.IsNullOrWhiteSpace(row.OptionAText) && string.IsNullOrWhiteSpace(row.OptionBText)
                && string.IsNullOrWhiteSpace(row.OptionCText) && string.IsNullOrWhiteSpace(row.OptionDText);

            string difficulty = row.Difficulty switch
            {
                1 => "easy",
                2 => "medium",
                _ => "hard"
            };

            string status = (row.Status?.ToLower()) switch
            {
                "published" => "published",
                _ => "draft"
            };

            await writer.StartRowAsync();
            await writer.WriteAsync(questionId, NpgsqlTypes.NpgsqlDbType.Uuid);
            await writer.WriteAsync(examId, NpgsqlTypes.NpgsqlDbType.Uuid);
            await writer.WriteAsync(standardId, NpgsqlTypes.NpgsqlDbType.Uuid);
            await writer.WriteAsync(subjectId, NpgsqlTypes.NpgsqlDbType.Uuid);
            await writer.WriteAsync(topicId, NpgsqlTypes.NpgsqlDbType.Uuid);  // NEW
            await writer.WriteAsync(conceptId, NpgsqlTypes.NpgsqlDbType.Uuid); // NEW
            await writer.WriteAsync((object?)row.QuestionText ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
            await writer.WriteAsync((object?)row.QuestionImageUrl ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
            await writer.WriteAsync(hasImageOptions, NpgsqlTypes.NpgsqlDbType.Boolean);
            await writer.WriteAsync(difficulty, NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync(status, NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync((object?)row.Explanation ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
            await writer.WriteAsync(hash, NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync((object?)createdById ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
            await writer.WriteAsync(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        }

        await writer.CompleteAsync();
    }

    private async Task BulkInsertOptionsAsync(
        Infrastructure.AppDb db, NpgsqlTransaction tx,
        List<(QuestionRow row, Guid questionId, string hash, Guid examId, Guid standardId, Guid subjectId, Guid? topicId, Guid? conceptId)> validRows)
    {
        var conn = db.Connection!;

        using var writer = await conn.BeginBinaryImportAsync(
            @"COPY options (id, question_id, label, text, image_url, is_correct, position) 
            FROM STDIN (FORMAT BINARY)");

        foreach (var (row, questionId, _, _, _, _, _, _) in validRows)
        {
            var options = new (string Label, string? Text, string? ImageUrl, bool IsCorrect)[]
            {
                ("A", row.OptionAText, row.OptionAImageUrl, row.CorrectOption == "A"),
                ("B", row.OptionBText, row.OptionBImageUrl, row.CorrectOption == "B"),
                ("C", row.OptionCText, row.OptionCImageUrl, row.CorrectOption == "C"),
                ("D", row.OptionDText, row.OptionDImageUrl, row.CorrectOption == "D"),
            };

            int position = 0;
            foreach (var (label, text, imgUrl, isCorrect) in options)
            {
                var optId = Guid.NewGuid();
                await writer.StartRowAsync();
                await writer.WriteAsync(optId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(questionId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(label, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync((object?)text ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync((object?)imgUrl ?? DBNull.Value, NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(isCorrect, NpgsqlTypes.NpgsqlDbType.Boolean);
                await writer.WriteAsync(position++, NpgsqlTypes.NpgsqlDbType.Smallint);
            }
        }

        await writer.CompleteAsync();
    }

    private async Task BulkInsertAssociationsAsync(
        Infrastructure.AppDb db, NpgsqlTransaction tx,
        List<(QuestionRow row, Guid questionId, string hash, Guid examId, Guid standardId, Guid subjectId, Guid? topicId, Guid? conceptId)> validRows)
    {
        var conn = db.Connection!;

        // Bulk insert question_topics
        var rowsWithTopics = validRows.Where(r => r.topicId.HasValue).ToList();
        if (rowsWithTopics.Count > 0)
        {
            using var writer = await conn.BeginBinaryImportAsync(
                @"COPY question_topics (question_id, topic_id) FROM STDIN (FORMAT BINARY)");

            foreach (var (_, questionId, _, _, _, _, topicId, _) in rowsWithTopics)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(questionId, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(topicId!.Value, NpgsqlTypes.NpgsqlDbType.Uuid);
            }
            await writer.CompleteAsync();
        }

        // Bulk insert question_skills
        var skillRows = new List<(Guid questionId, Guid skillId)>();
        foreach (var (row, questionId, _, _, _, _, _, _) in validRows)
        {
            var skillNames = (row.ThinkingSkills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToArray();
            var skillIds = db.GetSkillIds(skillNames);
            foreach (var sid in skillIds)
                skillRows.Add((questionId, sid));
        }

        if (skillRows.Count > 0)
        {
            using var writer = await conn.BeginBinaryImportAsync(
                @"COPY question_skills (question_id, skill_id) FROM STDIN (FORMAT BINARY)");

            foreach (var (qid, sid) in skillRows)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(qid, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(sid, NpgsqlTypes.NpgsqlDbType.Uuid);
            }
            await writer.CompleteAsync();
        }

        // Bulk insert question_images
        var imageRows = new List<(Guid questionId, string url, string type, int order)>();
        foreach (var (row, questionId, _, _, _, _, _, _) in validRows)
        {
            if (!string.IsNullOrWhiteSpace(row.QuestionImageUrl))
                imageRows.Add((questionId, row.QuestionImageUrl, "question", 0));

            if (!string.IsNullOrWhiteSpace(row.AdditionalImageUrls))
            {
                var extraUrls = row.AdditionalImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < extraUrls.Length; i++)
                {
                    var url = extraUrls[i].Trim();
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    string imgType = url.Contains("hint") ? "hint" : "reference";
                    imageRows.Add((questionId, url, imgType, i + 1));
                }
            }
        }

        if (imageRows.Count > 0)
        {
            using var writer = await conn.BeginBinaryImportAsync(
                @"COPY question_images (question_id, image_url, image_type, display_order) 
                FROM STDIN (FORMAT BINARY)");

            foreach (var (qid, url, type, ord) in imageRows)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(qid, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(url, NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(type, NpgsqlTypes.NpgsqlDbType.Varchar);
                await writer.WriteAsync(ord, NpgsqlTypes.NpgsqlDbType.Smallint);
            }
            await writer.CompleteAsync();
        }
    }

    // --- Legacy single-row insert (kept for reference) ---
    private async Task ImportRowFastAsync(
        Infrastructure.AppDb db, NpgsqlTransaction tx,
        QuestionRow row, Dictionary<string, string> imageMap,
        Guid? createdById, BulkUploadResult result)
    {
        // Same validation as PrepareRowAsync...
        // Uses single INSERTs (slower, kept for ZIP upload path)
        // ... (your existing logic)
    }

    // --- Excel Parsing ---
    private List<QuestionRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Question Upload")
            ?? workbook.Worksheet(1);

        var rows = new List<QuestionRow>();
        var range = ws.RangeUsed();
        int startRow = range.FirstRow().RowNumber() + 4; // skip title + subtitle + header

        for (int r = startRow; r <= range.LastRow().RowNumber(); r++)
        {
            var sno = ws.Cell(r, 2).GetValue<int>();
            if (sno == 0) continue;

            rows.Add(new QuestionRow
            {
                SNo = sno,
                Exam = ws.Cell(r, 3).GetString(),
                Class = ws.Cell(r, 4).GetString(),
                Subject = ws.Cell(r, 5).GetString(),
                Topic = ws.Cell(r, 6).GetString(),
                Subtopic = ws.Cell(r, 7).GetString(),
                Concept = ws.Cell(r, 8).GetString(),
                ThinkingSkills = ws.Cell(r, 9).GetString(),
                Difficulty = ws.Cell(r, 10).GetValue<int>(),
                QuestionText = ws.Cell(r, 11).GetString(),
                AdditionalImageUrls = ws.Cell(r, 13).GetString(),
                OptionAText = ws.Cell(r, 14).GetString(),
                OptionAImageUrl = ws.Cell(r, 15).GetString(),
                OptionBText = ws.Cell(r, 16).GetString(),
                OptionBImageUrl = ws.Cell(r, 17).GetString(),
                OptionCText = ws.Cell(r, 18).GetString(),
                OptionCImageUrl = ws.Cell(r, 19).GetString(),
                OptionDText = ws.Cell(r, 20).GetString(),
                OptionDImageUrl = ws.Cell(r, 21).GetString(),
                CorrectOption = ws.Cell(r, 22).GetString()?.Trim().ToUpper(),
                Explanation = ws.Cell(r, 23).GetString(),
                Status = ws.Cell(r, 24).GetString()?.Trim()
            });
        }
        return rows;
    }

    private string? ResolveImageUrl(string? cellValue, int sno, string imageKey, Dictionary<string, string> imageMap)
    {
        if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return cellValue.Trim();

        var zipKey = $"Q{sno}_{imageKey}";
        foreach (var kvp in imageMap)
        {
            var fileName = Path.GetFileNameWithoutExtension(kvp.Key);
            if (fileName.Equals(zipKey, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return null;
    }

    private static string ComputeHash(params string[] parts)
    {
        var normalized = parts.Select(p =>
            p?.ToLower()
              .Replace("[^a-z0-9s]", " ")
              .Replace("s+", " ")
              .Trim() ?? ""
        ).ToArray();

        var combined = string.Join("|||", normalized);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}