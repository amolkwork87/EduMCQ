using Npgsql;
using System.Data;

namespace EduMCQ.Infrastructure;

public class AppDb : IDisposable
{
    private readonly NpgsqlConnection _conn;

    public AppDb(string connectionString)
    {
        _conn = new NpgsqlConnection(connectionString);
        _conn.Open();
    }

    public NpgsqlConnection Connection => _conn;

    public NpgsqlCommand CreateCommand(string sql)
    {
        var cmd = new NpgsqlCommand(sql, _conn);
        cmd.CommandTimeout = 300; // 5 min for bulk ops
        return cmd;
    }

    // Look up hierarchy IDs by name — returns null if not found
    public async Task<Guid?> GetExamIdAsync(NpgsqlTransaction tx, string name)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM exams WHERE name = @name", _conn, tx);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<Guid?> GetStandardIdAsync(NpgsqlTransaction tx, Guid examId, string name)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM standards WHERE exam_id = @eid AND name = @name", _conn, tx);
        cmd.Parameters.AddWithValue("eid", examId);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<Guid?> GetSubjectIdAsync(NpgsqlTransaction tx, Guid examId, string name)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM subjects WHERE exam_id = @eid AND name = @name", _conn, tx);
        cmd.Parameters.AddWithValue("eid", examId);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    // Find topic by subject + name + optional parent
    public async Task<Guid?> GetTopicIdAsync(NpgsqlTransaction tx, Guid subjectId, string name, string? subtopicName = null)
    {
        if (subtopicName == null)
        {
            // Top-level topic
            using var cmd = new NpgsqlCommand(
                "SELECT id FROM topics WHERE subject_id = @sid AND name = @name AND parent_id IS NULL", _conn, tx);
            cmd.Parameters.AddWithValue("sid", subjectId);
            cmd.Parameters.AddWithValue("name", name);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : (Guid?)result;
        }
        else
        {
            // Find parent topic first, then subtopic
            using var parentCmd = new NpgsqlCommand(
                "SELECT id FROM topics WHERE subject_id = @sid AND name = @pname AND parent_id IS NULL", _conn, tx);
            parentCmd.Parameters.AddWithValue("sid", subjectId);
            parentCmd.Parameters.AddWithValue("pname", name);
            var parentId = await parentCmd.ExecuteScalarAsync();
            if (parentId == null) return null;

            using var childCmd = new NpgsqlCommand(
                "SELECT id FROM topics WHERE parent_id = @pid AND name = @cname", _conn, tx);
            childCmd.Parameters.AddWithValue("pid", (Guid)parentId);
            childCmd.Parameters.AddWithValue("cname", subtopicName);
            var childResult = await childCmd.ExecuteScalarAsync();
            return childResult == null ? null : (Guid?)childResult;
        }
    }

    public async Task<Guid?> GetConceptIdAsync(NpgsqlTransaction tx, Guid topicId, string name)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT id FROM concepts WHERE topic_id = @tid AND name = @name", _conn, tx);
        cmd.Parameters.AddWithValue("tid", topicId);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<List<Guid>> GetSkillIdsAsync(NpgsqlTransaction tx, string[] skillNames)
    {
        var ids = new List<Guid>();
        foreach (var name in skillNames)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT id FROM thinking_skills WHERE LOWER(name) = LOWER(@name)", _conn, tx);
            cmd.Parameters.AddWithValue("name", name.Trim());
            var result = await cmd.ExecuteScalarAsync();
            if (result != null) ids.Add((Guid)result);
        }
        return ids;
    }

    // Check for duplicate by content hash
    public async Task<Guid?> FindDuplicateAsync(NpgsqlTransaction tx, string contentHash)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT id FROM questions WHERE content_hash = @hash", _conn, tx);
        cmd.Parameters.AddWithValue("hash", contentHash);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public void Dispose() => _conn.Dispose();
}