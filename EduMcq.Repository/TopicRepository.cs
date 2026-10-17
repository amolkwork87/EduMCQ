using EduMcq.Interfaces;
using EduMcq.Models;
using MySqlConnector;

namespace EduMcq.Repository;

public class TopicRepository : ITopicRepository
{
    private const string Columns = "id, subject_id, parent_id, name, slug, depth_level, display_order, is_active, created_at";
    private readonly string _connectionString;

    public TopicRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<List<Topic>> GetTopicsAsync(Guid subjectId, Guid? parentId, bool activeOnly = true)
    {
        var result = new List<Topic>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(@"WITH RECURSIVE topic_tree AS (
            SELECT id, subject_id, parent_id, name, slug, depth_level, display_order, is_active, created_at, 0 as level
            FROM topics WHERE subject_id = @subjectId AND parent_id IS NULL
            UNION ALL
            SELECT t.id, t.subject_id, t.parent_id, t.name, t.slug, t.depth_level, t.display_order, t.is_active, t.created_at, tt.level + 1
            FROM topics t INNER JOIN topic_tree tt ON t.parent_id = tt.id)
            SELECT id, subject_id, parent_id, name, slug, depth_level, display_order, is_active, created_at FROM topic_tree
            WHERE (@activeOnly = false OR is_active = true) AND (@parentId IS NULL OR parent_id = @parentId OR id = @parentId)
            ORDER BY level, display_order, name", connection);
        command.Parameters.AddWithValue("subjectId", subjectId);
        command.Parameters.AddWithValue("activeOnly", activeOnly);
        // Explicitly typed parameter for UUID
        var parentIdParam = new MySqlParameter("parentId", MySqlDbType.Guid)
        {
            Value = (object?)parentId ?? DBNull.Value
        };
        command.Parameters.Add(parentIdParam);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(Map(reader));
        return result;
    }

    public async Task<List<Topic>> GetTopicTreeAsync(Guid subjectId)
    {
        var result = new List<Topic>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(@"WITH RECURSIVE topic_tree AS (
            SELECT id, subject_id, parent_id, name, slug, depth_level, display_order, is_active, created_at, CAST(name AS CHAR(2000)) AS path
            FROM topics WHERE subject_id = @subjectId AND parent_id IS NULL AND is_active = true
            UNION ALL
            SELECT t.id, t.subject_id, t.parent_id, t.name, t.slug, t.depth_level, t.display_order, t.is_active, t.created_at, CONCAT(tt.path, ' > ', t.name)
            FROM topics t INNER JOIN topic_tree tt ON t.parent_id = tt.id WHERE t.is_active = true)
            SELECT id, subject_id, parent_id, name, slug, depth_level, display_order, is_active, created_at FROM topic_tree ORDER BY path", connection);
        command.Parameters.AddWithValue("subjectId", subjectId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(Map(reader));
        return result;
    }

    public async Task<Topic> CreateTopic(Guid subjectId, CreateTopicRequest req)
    {
        var id = Guid.NewGuid();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand($"INSERT INTO topics (id, subject_id, parent_id, name, slug, depth_level, display_order, is_active) VALUES (@id, @subjectId, @parentId, @name, @slug, @depth, @display_order, @active)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("subjectId", subjectId);
        command.Parameters.AddWithValue("parentId", (object?)req.ParentId ?? DBNull.Value);
        command.Parameters.AddWithValue("name", req.Name.Trim());
        command.Parameters.AddWithValue("slug", string.IsNullOrWhiteSpace(req.Slug) ? GenerateSlug(req.Name) : req.Slug);
        command.Parameters.AddWithValue("depth", (short)(req.ParentId.HasValue ? 2 : 1));
        command.Parameters.AddWithValue("display_order", (object?)req.DisplayOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("active", req.IsActive ?? true);
        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand($"SELECT {Columns} FROM topics WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        await reader.ReadAsync();
        return Map(reader);
    }

    public async Task<Topic?> UpdateTopic(Guid id, UpdateTopicRequest req)
    {
        var updates = new List<string>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand { Connection = connection };
        if (req.Name != null) { updates.Add("name = @name"); command.Parameters.AddWithValue("name", req.Name.Trim()); }
        if (req.Slug != null) { updates.Add("slug = @slug"); command.Parameters.AddWithValue("slug", req.Slug); }
        if (req.ParentId.HasValue) { updates.Add("parent_id = @parentId"); command.Parameters.AddWithValue("parentId", req.ParentId.Value); }
        if (req.DisplayOrder.HasValue) { updates.Add("display_order = @display_order"); command.Parameters.AddWithValue("display_order", req.DisplayOrder.Value); }
        if (req.IsActive.HasValue) { updates.Add("is_active = @active"); command.Parameters.AddWithValue("active", req.IsActive.Value); }
        if (updates.Count == 0) return null;
        command.CommandText = $"UPDATE topics SET {string.Join(", ", updates)} WHERE id = @id";
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand($"SELECT {Columns} FROM topics WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> DeleteTopic(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("DELETE FROM topics WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static string GenerateSlug(string name) => name.ToLowerInvariant().Replace(" ", "-").Replace("&", "and").Replace(",", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("'", "").Replace("--", "-");
    private static Topic Map(MySqlDataReader r) => new() { Id = r.GetGuid(0), SubjectId = r.GetGuid(1), ParentId = r.IsDBNull(2) ? null : r.GetGuid(2), Name = r.GetString(3), Slug = r.GetString(4), DepthLevel = r.GetInt16(5), DisplayOrder = r.GetInt16(6), IsActive = r.GetBoolean(7), CreatedAt = r.GetDateTime(8) };
}