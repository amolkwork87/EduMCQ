using EduMcq.Models;
using EduMcq.Interfaces;
using Npgsql;

namespace EduMcq.Repository;

public class ConceptRepository : IConceptRepository
{
    private const string Columns = "id, topic_id, name, description, display_order, is_active, created_at";
    private readonly string _connectionString;

    public ConceptRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<List<Concept>> GetConceptsAsync(Guid topicId, bool activeOnly = true)
    {
        var result = new List<Concept>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var sql = $"SELECT {Columns} FROM concepts WHERE topic_id = @topicId" + (activeOnly ? " AND is_active = true" : "") + " ORDER BY display_order, name";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("topicId", topicId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(Map(reader));
        return result;
    }

    public async Task<Concept> CreateConcept(Guid topicId, CreateConceptRequest req)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($@"INSERT INTO concepts (topic_id, name, description, display_order, is_active)
            VALUES (@topicId, @name, @description, @display_order, @active) RETURNING {Columns}", connection);
        command.Parameters.AddWithValue("topicId", topicId);
        command.Parameters.AddWithValue("name", req.Name.Trim());
        command.Parameters.AddWithValue("description", (object?)req.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("display_order", (object?)req.DisplayOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("active", req.IsActive ?? true);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return Map(reader);
    }

    public async Task<Concept?> UpdateConcept(Guid id, UpdateConceptRequest req)
    {
        var updates = new List<string>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand { Connection = connection };
        if (req.Name != null) { updates.Add("name = @name"); command.Parameters.AddWithValue("name", req.Name.Trim()); }
        if (req.Description != null) { updates.Add("description = @description"); command.Parameters.AddWithValue("description", req.Description); }
        if (req.DisplayOrder.HasValue) { updates.Add("display_order = @display_order"); command.Parameters.AddWithValue("display_order", req.DisplayOrder.Value); }
        if (req.IsActive.HasValue) { updates.Add("is_active = @active"); command.Parameters.AddWithValue("active", req.IsActive.Value); }
        if (updates.Count == 0) return null;
        command.CommandText = $"UPDATE concepts SET {string.Join(", ", updates)} WHERE id = @id RETURNING {Columns}";
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> DeleteConcept(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM concepts WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static Concept Map(NpgsqlDataReader r) => new() { Id = r.GetGuid(0), TopicId = r.GetGuid(1), Name = r.GetString(2), Description = r.IsDBNull(3) ? null : r.GetString(3), DisplayOrder = r.GetInt16(4), IsActive = r.GetBoolean(5), CreatedAt = r.GetDateTime(6) };

}