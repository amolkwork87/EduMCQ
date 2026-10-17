using EduMcq.Interfaces;
using EduMcq.Models;
using MySqlConnector;

namespace EduMcq.Repository;

public class ExamRepository : IExamRepository
{
    private const string ExamColumns = "id, name, slug, description, logo_url, is_active, display_order, created_at";
    private readonly string _connectionString;

    public ExamRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<List<Exam>> GetExamsAsync(bool activeOnly = true)
    {
        var exams = new List<Exam>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        var sql = $"SELECT {ExamColumns} FROM exams" +
                  (activeOnly ? " WHERE is_active = true" : "") +
                  " ORDER BY display_order, name";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            exams.Add(MapExam(reader));
        return exams;
    }

    public async Task<Exam?> GetExamByIdAsync(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand($"SELECT {ExamColumns} FROM exams WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapExam(reader) : null;
    }

    public async Task<Exam> CreateExam(CreateExamRequest req)
    {
        var id = Guid.NewGuid();
        var slug = string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(req.Name) : req.Slug;
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand($@"
            INSERT INTO exams (id, name, slug, description, logo_url, is_active, display_order)
            VALUES (@id, @name, @slug, @description, @logo, @active, @display_order)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", req.Name.Trim());
        command.Parameters.AddWithValue("slug", slug);
        command.Parameters.AddWithValue("description", (object?)req.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("logo", (object?)req.LogoUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("active", req.IsActive ?? true);
        command.Parameters.AddWithValue("display_order", (object?)req.DisplayOrder ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand($"SELECT {ExamColumns} FROM exams WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapExam(reader);
    }

    public async Task<Exam?> UpdateExam(Guid id, UpdateExamRequest req)
    {
        var updates = new List<string>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand { Connection = connection };
        if (req.Name != null) { updates.Add("name = @name"); command.Parameters.AddWithValue("name", req.Name.Trim()); }
        if (req.Slug != null) { updates.Add("slug = @slug"); command.Parameters.AddWithValue("slug", req.Slug); }
        if (req.Description != null) { updates.Add("description = @description"); command.Parameters.AddWithValue("description", req.Description); }
        if (req.LogoUrl != null) { updates.Add("logo_url = @logo"); command.Parameters.AddWithValue("logo", req.LogoUrl); }
        if (req.IsActive.HasValue) { updates.Add("is_active = @active"); command.Parameters.AddWithValue("active", req.IsActive.Value); }
        if (req.DisplayOrder.HasValue) { updates.Add("display_order = @display_order"); command.Parameters.AddWithValue("display_order", req.DisplayOrder.Value); }
        if (updates.Count == 0)
            return null;
        command.CommandText = $"UPDATE exams SET {string.Join(", ", updates)} WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand($"SELECT {ExamColumns} FROM exams WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapExam(reader) : null;
    }

    public async Task<bool> DeleteExam(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("DELETE FROM exams WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static string GenerateSlug(string name) => name.ToLowerInvariant()
        .Replace(" ", "-").Replace("&", "and").Replace(",", "")
        .Replace(".", "").Replace("(", "").Replace(")", "")
        .Replace("'", "").Replace("--", "-");

    private static Exam MapExam(MySqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Name = reader.GetString(1),
        Slug = reader.GetString(2),
        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
        LogoUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
        IsActive = reader.GetBoolean(5),
        DisplayOrder = reader.GetInt16(6),
        CreatedAt = reader.GetDateTime(7)
    };
}