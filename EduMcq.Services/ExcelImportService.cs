using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using EduMcq.Interfaces;
using EduMcq.Models.BulkUpload;
using Microsoft.Extensions.Logging;

namespace EduMcq.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly ILogger<ExcelImportService> _logger;
    private readonly IExcelImportRepository _excelImportRepository;
    private readonly Infrastructure.AppDb _db;

    public ExcelImportService(
        ILogger<ExcelImportService> logger,
        IExcelImportRepository excelImportRepository,
        Infrastructure.AppDb db)
    {
        _logger = logger;
        _excelImportRepository = excelImportRepository;
        _db = db;
    }

    // public async Task<BulkUploadResult> ImportAsync(Stream excelStream, Dictionary<string, string>? imageMap = null, Guid? createdById = null)
    // {
    //     var result = new BulkUploadResult();
    //     var rows = ParseExcel(excelStream);
    //     result.TotalRows = rows.Count;
    //     return result;
    // }

    public async Task<BatchUploadResponse> ImportBatchAsync(List<QuestionRow> rows, List<string> duplicateHashes, Guid? createdById = null)
    {
        var response = new BatchUploadResponse();
        var newHashes = new List<string>();
        var validRows = new List<PreparedQuestion>();
        var connection = await _db.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await _db.PreloadCachesAsync(transaction);

        foreach (var row in rows)
        {
            try
            {
                var prepared = PrepareRow(_db, row, duplicateHashes, newHashes, createdById);
                if (prepared == null)
                {
                    response.DuplicateCount++;
                    response.Duplicates.Add(new DuplicateRow { RowNumber = row.SNo, QuestionText = row.QuestionText ?? "", ExistingQuestionId = Guid.Empty });
                }
                else validRows.Add(prepared);
            }
            catch (Exception exception)
            {
                response.FailureCount++;
                response.Errors.Add(new FailedRow { RowNumber = row.SNo, QuestionText = row.QuestionText ?? "", Error = exception.Message });
            }
        }

        await transaction.CommitAsync();
        await _excelImportRepository.PersistBatchAsync(validRows, createdById);
        response.SuccessCount = validRows.Count;
        response.NewHashes = newHashes;
        response.Status = response.FailureCount > 0 ? "Partial" : "Completed";
        response.Message = $"Processed {rows.Count}: {response.SuccessCount} success, {response.DuplicateCount} duplicates, {response.FailureCount} failed";
        return response;
    }

    private PreparedQuestion? PrepareRow(Infrastructure.AppDb db, QuestionRow row, List<string> duplicateHashes, List<string> newHashes, Guid? createdById)
    {
        if (string.IsNullOrWhiteSpace(row.QuestionText) && string.IsNullOrWhiteSpace(row.QuestionImageUrl))
            throw new InvalidOperationException("Question text and image URL are both empty.");
        if (row.CorrectOption is not ("A" or "B" or "C" or "D"))
            throw new InvalidOperationException($"Invalid Correct_Option: '{row.CorrectOption}'. Must be A, B, C, or D.");
        if (row.Difficulty is not (1 or 2 or 3))
            throw new InvalidOperationException($"Invalid Difficulty: {row.Difficulty}. Must be 1, 2, or 3.");
        if (new[] { row.OptionAText, row.OptionBText, row.OptionCText, row.OptionDText, row.OptionAImageUrl, row.OptionBImageUrl, row.OptionCImageUrl, row.OptionDImageUrl }.All(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("All 4 options are empty.");

        var hash = ComputeHash(row.QuestionText ?? "", row.OptionAText ?? "", row.OptionBText ?? "", row.OptionCText ?? "", row.OptionDText ?? "");
        if (db.IsDuplicateHash(hash) || duplicateHashes.Contains(hash)) return null;
        var examId = db.GetExamId(row.Exam ?? "") ?? throw new InvalidOperationException($"Exam not found: '{row.Exam}'");
        var standardId = db.GetStandardId(examId, row.Class ?? "") ?? throw new InvalidOperationException($"Class not found: '{row.Class}'");
        var subjectId = db.GetSubjectId(examId, row.Subject ?? "") ?? throw new InvalidOperationException($"Subject not found: '{row.Subject}'");
        Guid? topicId = null;
        Guid? conceptId = null;
        if (!string.IsNullOrWhiteSpace(row.Topic))
        {
            topicId = db.GetTopicId(subjectId, row.Topic, row.Subtopic);
            if (!topicId.HasValue) throw new InvalidOperationException($"Topic not found: '{row.Topic}'");
            //if (!string.IsNullOrWhiteSpace(row.Concept)) conceptId = db.GetConceptId(topicId.Value, row.Concept) ?? throw new InvalidOperationException($"Concept not found: '{row.Concept}'");
        }
        else if (!string.IsNullOrWhiteSpace(row.Concept)) throw new InvalidOperationException("Concept provided but Topic is missing");
        var prepared = new PreparedQuestion(row, Guid.NewGuid(), hash, examId, standardId, subjectId, topicId, conceptId);
        newHashes.Add(hash);
        db.AddHash(hash);
        return prepared;
    }

    private static List<QuestionRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == "Question Upload") ?? workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        var rows = new List<QuestionRow>();
        for (var rowNumber = range.FirstRow().RowNumber() + 4; rowNumber <= range.LastRow().RowNumber(); rowNumber++)
        {
            var sno = worksheet.Cell(rowNumber, 2).GetValue<int>();
            if (sno == 0) continue;
            rows.Add(new QuestionRow
            {
                SNo = sno, Exam = worksheet.Cell(rowNumber, 3).GetString(), Class = worksheet.Cell(rowNumber, 4).GetString(), Subject = worksheet.Cell(rowNumber, 5).GetString(), Topic = worksheet.Cell(rowNumber, 6).GetString(), Subtopic = worksheet.Cell(rowNumber, 7).GetString(), Concept = worksheet.Cell(rowNumber, 8).GetString(), ThinkingSkills = worksheet.Cell(rowNumber, 9).GetString(), Difficulty = worksheet.Cell(rowNumber, 10).GetValue<int>(), QuestionText = worksheet.Cell(rowNumber, 11).GetString(), AdditionalImageUrls = worksheet.Cell(rowNumber, 13).GetString(), OptionAText = worksheet.Cell(rowNumber, 14).GetString(), OptionAImageUrl = worksheet.Cell(rowNumber, 15).GetString(), OptionBText = worksheet.Cell(rowNumber, 16).GetString(), OptionBImageUrl = worksheet.Cell(rowNumber, 17).GetString(), OptionCText = worksheet.Cell(rowNumber, 18).GetString(), OptionCImageUrl = worksheet.Cell(rowNumber, 19).GetString(), OptionDText = worksheet.Cell(rowNumber, 20).GetString(), OptionDImageUrl = worksheet.Cell(rowNumber, 21).GetString(), CorrectOption = worksheet.Cell(rowNumber, 22).GetString().Trim().ToUpperInvariant(), Explanation = worksheet.Cell(rowNumber, 23).GetString(), Status = worksheet.Cell(rowNumber, 24).GetString().Trim()
            });
        }
        return rows;
    }

    private static string ComputeHash(params string[] parts)
    {
        var normalized = parts.Select(part => new string((part ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray())).ToArray();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|||", normalized)))).ToLowerInvariant();
    }
}
