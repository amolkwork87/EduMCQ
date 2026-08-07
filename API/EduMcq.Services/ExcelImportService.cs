
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using Npgsql;

namespace EduMCQ.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly string _connectionString;

    public ExcelImportService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Parses an Excel file (from ZIP or direct upload) and bulk-inserts questions.
    /// imageMap: dictionary of filename -> CDN URL (from ZIP extraction)
    /// </summary>
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
        await using var tx = await db.Connection.BeginTransactionAsync();

        try
        {
            foreach (var row in rows)
            {
                try
                {
                    await ImportRowAsync(db, tx, row, imageMap, createdById, result);
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
                Exam = ws.Cell(r, 3).GetString(),          // C
                Class = ws.Cell(r, 4).GetString(),          // D
                Subject = ws.Cell(r, 5).GetString(),        // E
                Topic = ws.Cell(r, 6).GetString(),          // F
                Subtopic = ws.Cell(r, 7).GetString(),       // G
                Concept = ws.Cell(r, 8).GetString(),        // H
                ThinkingSkills = ws.Cell(r, 9).GetString(), // I
                Difficulty = ws.Cell(r, 10).GetValue<int>(), // J
                QuestionText = ws.Cell(r, 11).GetString(),   // K
                //QuestionImageUrl = ResolveImage(ws.Cells[r, 12].Text, sno, "question", imageMap: null), // L - resolved later
                AdditionalImageUrls = ws.Cell(r, 13).GetString(), // M
                OptionAText = ws.Cell(r, 14).GetString(),    // N
                OptionAImageUrl = ws.Cell(r, 15).GetString(), // O
                OptionBText = ws.Cell(r, 16).GetString(),    // P
                OptionBImageUrl = ws.Cell(r, 17).GetString(), // Q
                OptionCText = ws.Cell(r, 18).GetString(),    // R
                OptionCImageUrl = ws.Cell(r, 19).GetString(), // S
                OptionDText = ws.Cell(r, 20).GetString(),    // T
                OptionDImageUrl = ws.Cell(r, 21).GetString(), // U
                CorrectOption = ws.Cell(r, 22).GetString()?.Trim().ToUpper(), // V
                Explanation = ws.Cell(r, 23).GetString(),    // W
                Status = ws.Cell(r, 24).GetString()?.Trim()  // X
            });
        }
        return rows;
    }

    private async Task ImportRowAsync(
        Infrastructure.AppDb db, NpgsqlTransaction tx,
        QuestionRow row, Dictionary<string, string> imageMap,
        Guid? createdById, BulkUploadResult result)
    {
        // --- Validation ---
        if (string.IsNullOrWhiteSpace(row.QuestionText) && string.IsNullOrWhiteSpace(row.QuestionImageUrl))
            throw new Exception("Question text and image URL are both empty.");
        if (row.CorrectOption is not ("A" or "B" or "C" or "D"))
            throw new Exception($"Invalid Correct_Option: '{row.CorrectOption}'. Must be A, B, C, or D.");
        if (row.Difficulty is not (1 or 2 or 3))
            throw new Exception($"Invalid Difficulty: {row.Difficulty}. Must be 1, 2, or 3.");

        // Validate at least one option has content
        var hasAnyOption = !string.IsNullOrWhiteSpace(row.OptionAText) || !string.IsNullOrWhiteSpace(row.OptionAImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionBText) || !string.IsNullOrWhiteSpace(row.OptionBImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionCText) || !string.IsNullOrWhiteSpace(row.OptionCImageUrl)
                || !string.IsNullOrWhiteSpace(row.OptionDText) || !string.IsNullOrWhiteSpace(row.OptionDImageUrl);
        if (!hasAnyOption) throw new Exception("All 4 options are empty.");

        // --- Resolve image URLs from ZIP map ---
        row.QuestionImageUrl = ResolveImageUrl(row.QuestionImageUrl, row.SNo, "question", imageMap);
        row.OptionAImageUrl = ResolveImageUrl(row.OptionAImageUrl, row.SNo, "optA", imageMap);
        row.OptionBImageUrl = ResolveImageUrl(row.OptionBImageUrl, row.SNo, "optB", imageMap);
        row.OptionCImageUrl = ResolveImageUrl(row.OptionCImageUrl, row.SNo, "optC", imageMap);
        row.OptionDImageUrl = ResolveImageUrl(row.OptionDImageUrl, row.SNo, "optD", imageMap);

        // --- Resolve hierarchy IDs ---
        var examId = await db.GetExamIdAsync(tx, row.Exam ?? "")
            ?? throw new Exception($"Exam not found: '{row.Exam}'");
        var standardId = await db.GetStandardIdAsync(tx, examId, row.Class ?? "")
            ?? throw new Exception($"Class not found: '{row.Class}' under exam '{row.Exam}'");
        var subjectId = await db.GetSubjectIdAsync(tx, examId, row.Subject ?? "")
            ?? throw new Exception($"Subject not found: '{row.Subject}' under exam '{row.Exam}'");

        Guid? topicId = null;
        if (!string.IsNullOrWhiteSpace(row.Topic))
        {
            topicId = await db.GetTopicIdAsync(tx, subjectId, row.Topic, row.Subtopic)
                ?? throw new Exception($"Topic/Subtopic not found: '{row.Topic}' / '{row.Subtopic}'");
        }

        Guid? conceptId = null;
        if (!string.IsNullOrWhiteSpace(row.Concept) && topicId.HasValue)
        {
            conceptId = await db.GetConceptIdAsync(tx, topicId.Value, row.Concept)
                ?? throw new Exception($"Concept not found: '{row.Concept}' under topic '{row.Topic}'");
        }

        var skillNames = (row.ThinkingSkills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();
        var skillIds = await db.GetSkillIdsAsync(tx, skillNames);

        // --- Duplicate check ---
        var optTexts = new[] { row.OptionAText ?? "", row.OptionBText ?? "", row.OptionCText ?? "", row.OptionDText ?? "" };
        var contentHash = ComputeHash(row.QuestionText ?? "", optTexts[0], optTexts[1], optTexts[2], optTexts[3]);

        var existingId = await db.FindDuplicateAsync(tx, contentHash);
        if (existingId.HasValue)
        {
            result.DuplicateCount++;
            result.DuplicateRows.Add(new DuplicateRow
            {
                RowNumber = row.SNo,
                QuestionText = row.QuestionText ?? "",
                ExistingQuestionId = existingId.Value
            });
            return; // Skip this row
        }

        // --- Determine has_image_options ---
        bool hasImageOptions = string.IsNullOrWhiteSpace(row.OptionAText) && string.IsNullOrWhiteSpace(row.OptionBText)
            && string.IsNullOrWhiteSpace(row.OptionCText) && string.IsNullOrWhiteSpace(row.OptionDText);

        // --- Map difficulty ---
        string difficulty = row.Difficulty switch
        {
            1 => "easy",
            2 => "medium",
            _ => "hard"
        };

        // --- Map status ---
        string status = (row.Status?.ToLower()) switch
        {
            "published" => "published",
            _ => "draft"
        };

        // --- Insert question ---
        var questionId = Guid.NewGuid();
        using (var cmd = db.CreateCommand(@"
            INSERT INTO questions (id, exam_id, standard_id, subject_id, text, image_url,
                has_image_options, difficulty, status, explanation, content_hash, created_by)
            VALUES (@id, @examId, @stdId, @subjId, @text, @imgUrl, @hasImgOpt, @diff, @status, @expl, @hash, @createdBy)
            RETURNING id"))
        {
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("id", questionId);
            cmd.Parameters.AddWithValue("examId", examId);
            cmd.Parameters.AddWithValue("stdId", (object?)standardId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("subjId", (object?)subjectId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("text", (object?)row.QuestionText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imgUrl", (object?)row.QuestionImageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("hasImgOpt", hasImageOptions);
            cmd.Parameters.AddWithValue("diff", difficulty);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("expl", (object?)row.Explanation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("hash", contentHash);
            cmd.Parameters.AddWithValue("createdBy", (object?)createdById ?? DBNull.Value);
            await cmd.ExecuteScalarAsync();
        }

        // --- Insert topic association ---
        if (topicId.HasValue)
        {
            using var cmd = db.CreateCommand("INSERT INTO question_topics (question_id, topic_id) VALUES (@qid, @tid) ON CONFLICT DO NOTHING");
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("qid", questionId);
            cmd.Parameters.AddWithValue("tid", topicId.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        // --- Insert skill associations ---
        foreach (var sid in skillIds)
        {
            using var cmd = db.CreateCommand("INSERT INTO question_skills (question_id, skill_id) VALUES (@qid, @sid) ON CONFLICT DO NOTHING");
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("qid", questionId);
            cmd.Parameters.AddWithValue("sid", sid);
            await cmd.ExecuteNonQueryAsync();
        }

        // --- Insert options ---
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
            using var cmd = db.CreateCommand(@"
                INSERT INTO options (id, question_id, label, text, image_url, is_correct, position)
                VALUES (@id, @qid, @label, @text, @imgUrl, @isCorrect, @pos)");
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("id", optId);
            cmd.Parameters.AddWithValue("qid", questionId);
            cmd.Parameters.AddWithValue("label", label);
            cmd.Parameters.AddWithValue("text", (object?)text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("imgUrl", (object?)imgUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isCorrect", isCorrect);
            cmd.Parameters.AddWithValue("pos", position++);
            await cmd.ExecuteNonQueryAsync();
        }

        // --- Insert question images (main + additional) ---
        if (!string.IsNullOrWhiteSpace(row.QuestionImageUrl))
        {
            using var cmd = db.CreateCommand("INSERT INTO question_images (question_id, image_url, image_type, display_order) VALUES (@qid, @url, 'question', 0)");
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("qid", questionId);
            cmd.Parameters.AddWithValue("url", row.QuestionImageUrl);
            await cmd.ExecuteNonQueryAsync();
        }

        if (!string.IsNullOrWhiteSpace(row.AdditionalImageUrls))
        {
            var extraUrls = row.AdditionalImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < extraUrls.Length; i++)
            {
                var url = extraUrls[i].Trim();
                if (string.IsNullOrWhiteSpace(url)) continue;
                string imgType = url.Contains("hint") ? "hint" : "reference";
                using var cmd = db.CreateCommand("INSERT INTO question_images (question_id, image_url, image_type, display_order) VALUES (@qid, @url, @type, @ord)");
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("qid", questionId);
                cmd.Parameters.AddWithValue("url", url);
                cmd.Parameters.AddWithValue("type", imgType);
                cmd.Parameters.AddWithValue("ord", i + 1);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        result.SuccessCount++;
    }

    /// <summary>
    /// If the Excel cell has a URL, use it. If blank, try to find a matching image from the ZIP map.
    /// ZIP naming: Q{SNo}_question.png, Q{SNo}_optA.png, etc.
    /// </summary>
    private string? ResolveImageUrl(string? cellValue, int sno, string imageKey, Dictionary<string, string> imageMap)
    {
        // If Excel already has a full HTTPS URL, use it directly
        if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return cellValue.Trim();

        // Otherwise, try to find from ZIP image map
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
        var combined = string.Join("|", parts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}