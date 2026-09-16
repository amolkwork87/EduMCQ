using EduMcq.Interfaces;
using EduMcq.Models;
using Npgsql;

namespace EduMcq.Repository;

public class StandardRepository : IStandardRepository
{
    private const string Columns = "id, exam_id, name, slug, display_order, is_active, created_at";
    private readonly string _connectionString;

    public StandardRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<List<Standard>> GetStandardsAsync(Guid examId, bool activeOnly = true)
    {
        var result = new List<Standard>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var sql = $"SELECT {Columns} FROM standards WHERE exam_id = @examId" + (activeOnly ? " AND is_active = true" : "") + " ORDER BY display_order, name";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("examId", examId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(Map(reader));
        return result;
    }

    public async Task<Standard> CreateStandard(Guid examId, CreateStandardRequest req)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"INSERT INTO standards (exam_id, name, slug, display_order, is_active) VALUES (@examId, @name, @slug, @display_order, @active) RETURNING {Columns}", connection);
        command.Parameters.AddWithValue("examId", examId);
        command.Parameters.AddWithValue("name", req.Name.Trim());
        command.Parameters.AddWithValue("slug", string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(req.Name) : req.Slug);
        command.Parameters.AddWithValue("display_order", (object?)req.DisplayOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("active", req.IsActive ?? true);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return Map(reader);
    }

    public async Task<Standard?> UpdateStandard(Guid id, UpdateStandardRequest req)
    {
        var updates = new List<string>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand { Connection = connection };
        if (req.Name != null) { updates.Add("name = @name"); command.Parameters.AddWithValue("name", req.Name.Trim()); }
        if (req.Slug != null) { updates.Add("slug = @slug"); command.Parameters.AddWithValue("slug", req.Slug); }
        if (req.DisplayOrder.HasValue) { updates.Add("display_order = @display_order"); command.Parameters.AddWithValue("display_order", req.DisplayOrder.Value); }
        if (req.IsActive.HasValue) { updates.Add("is_active = @active"); command.Parameters.AddWithValue("active", req.IsActive.Value); }
        if (updates.Count == 0) return null;
        command.CommandText = $"UPDATE standards SET {string.Join(", ", updates)} WHERE id = @id RETURNING {Columns}";
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> DeleteStandard(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM standards WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static string GenerateSlug(string name) => name.ToLowerInvariant().Replace(" ", "-").Replace("&", "and").Replace(",", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("'", "").Replace("--", "-");
    private static Standard Map(NpgsqlDataReader r) => new() { Id = r.GetGuid(0), ExamId = r.GetGuid(1), Name = r.GetString(2), Slug = r.GetString(3), DisplayOrder = r.GetInt16(4), IsActive = r.GetBoolean(5), CreatedAt = r.GetDateTime(6) };
}