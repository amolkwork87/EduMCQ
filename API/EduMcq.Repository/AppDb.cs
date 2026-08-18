using Npgsql;
using System.Data;

namespace EduMcq.Infrastructure;

public class AppDb : IDisposable
{
    private readonly string _connectionString;
    private NpgsqlConnection? _conn;
    private bool _isDisposed;

    public AppDb(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        if (_conn == null || _conn.State != ConnectionState.Open)
        {
            _conn = new NpgsqlConnection(_connectionString);
            await _conn.OpenAsync();
        }
        return _conn;
    }

    public NpgsqlConnection? Connection => _conn;

    public async Task<NpgsqlCommand> CreateCommandAsync(string sql, NpgsqlTransaction? tx = null)
    {
        var conn = await GetConnectionAsync();
        var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.CommandTimeout = 300; // 5 min for bulk ops
        return cmd;
    }

    public NpgsqlCommand CreateCommand(string sql, NpgsqlTransaction? tx = null)
    {
        if (_conn == null || _conn.State != ConnectionState.Open)
            throw new InvalidOperationException("Connection not open. Use CreateCommandAsync instead.");

        var cmd = new NpgsqlCommand(sql, _conn, tx);
        cmd.CommandTimeout = 300;
        return cmd;
    }

    // --- Batch-optimized lookup methods (cache results in memory) ---

    private Dictionary<string, Guid>? _examCache;
    private Dictionary<(Guid examId, string name), Guid>? _standardCache;
    private Dictionary<(Guid examId, string name), Guid>? _subjectCache;
    private Dictionary<(Guid subjectId, string name, string? parentName), Guid> _topicCache = new();
    private Dictionary<Guid, Dictionary<string, Guid>> _conceptCache = new();
    private Dictionary<string, Guid>? _skillCache;
    private HashSet<string>? _hashCache;

    public async Task PreloadCachesAsync(NpgsqlTransaction tx)
    {
        var conn = await GetConnectionAsync();

        // Preload all exams
        _examCache = new();
        using (var cmd = new NpgsqlCommand("SELECT id, name FROM exams", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                _examCache[reader.GetString(1)] = reader.GetGuid(0);
        }

        // Preload all standards
        _standardCache = new();
        using (var cmd = new NpgsqlCommand("SELECT id, exam_id, name FROM standards", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                _standardCache[(reader.GetGuid(1), reader.GetString(2))] = reader.GetGuid(0);
        }

        // Preload all subjects
        _subjectCache = new();
        using (var cmd = new NpgsqlCommand("SELECT id, exam_id, name FROM subjects", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                _subjectCache[(reader.GetGuid(1), reader.GetString(2))] = reader.GetGuid(0);
        }

        // Preload all topics
        _topicCache = new();
        using (var cmd = new NpgsqlCommand("SELECT t.id, t.subject_id, t.name, p.name as parent_name FROM topics t LEFT JOIN topics p ON t.parent_id = p.id WHERE t.is_active = true", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
        {
            var subjectId = reader.GetGuid(1);
            var name = reader.GetString(2);
            var parentName = reader.IsDBNull(3) ? null : reader.GetString(3);
            _topicCache[(subjectId, name, parentName)] = reader.GetGuid(0);
        }
        }

        // Preload all concepts
        _conceptCache = new();
        using (var cmd = new NpgsqlCommand("SELECT id, topic_id, name FROM concepts WHERE is_active = true", conn, tx))
        using (var conceptReader = await cmd.ExecuteReaderAsync())
        {
            while (await conceptReader.ReadAsync())
            {
                 var topicId = conceptReader.GetGuid(1);
            var name = conceptReader.GetString(2);
            
            if (!_conceptCache.ContainsKey(topicId))
                _conceptCache[topicId] = new Dictionary<string, Guid>();
            
            _conceptCache[topicId][name] = conceptReader.GetGuid(0);
            }
        }

        // Preload all thinking skills
        _skillCache = new();
        using (var cmd = new NpgsqlCommand("SELECT id, LOWER(name) as name FROM thinking_skills", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                _skillCache[reader.GetString(1)] = reader.GetGuid(0);
        }

        // Preload all existing hashes for duplicate check
        _hashCache = new();
        using (var cmd = new NpgsqlCommand("SELECT content_hash FROM questions", conn, tx))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                _hashCache.Add(reader.GetString(0));
        }
    }

    // --- Cached lookup methods (use after PreloadCachesAsync) ---

    public Guid? GetExamId(string name)
    {
        if (_examCache == null) throw new InvalidOperationException("Caches not loaded. Call PreloadCachesAsync first.");
        return _examCache.TryGetValue(name, out var id) ? id : null;
    }

    public Guid? GetStandardId(Guid examId, string name)
    {
        if (_standardCache == null) throw new InvalidOperationException("Caches not loaded.");
        return _standardCache.TryGetValue((examId, name), out var id) ? id : null;
    }

    public Guid? GetSubjectId(Guid examId, string name)
    {
        if (_subjectCache == null) throw new InvalidOperationException("Caches not loaded.");
        return _subjectCache.TryGetValue((examId, name), out var id) ? id : null;
    }

    public Guid? GetTopicId(Guid subjectId, string topicName, string? subtopicName = null)
    {
        // First try to find the subtopic (if provided)
        if (!string.IsNullOrWhiteSpace(subtopicName))
        {
            // Find the parent topic first
            var parentTopicId = GetTopicId(subjectId, topicName);
            if (parentTopicId.HasValue)
            {
                // Now find the subtopic under this parent
                var key = (subjectId, subtopicName.Trim(), topicName.Trim());
                if (_topicCache.TryGetValue(key, out var subtopicId))
                    return subtopicId;
            }
            return null;
        }

        // Just find the topic by name (no parent)
        var topicKey = (subjectId, topicName.Trim(), (string?)null);
        if (_topicCache.TryGetValue(topicKey, out var topicId))
            return topicId;

        return null;
    }

    public Guid? GetConceptId(Guid topicId, string conceptName)
    {
        if (_conceptCache.TryGetValue(topicId, out var concepts))
        {
            if (concepts.TryGetValue(conceptName.Trim(), out var conceptId))
                return conceptId;
        }
        return null;
    }

    public List<Guid> GetSkillIds(string[] skillNames)
    {
        if (_skillCache == null) throw new InvalidOperationException("Caches not loaded.");
        var ids = new List<Guid>();
        foreach (var name in skillNames)
        {
            var key = name.Trim().ToLower();
            if (_skillCache.TryGetValue(key, out var id))
                ids.Add(id);
        }
        return ids;
    }

    public bool IsDuplicateHash(string hash)
    {
        if (_hashCache == null) throw new InvalidOperationException("Caches not loaded.");
        return _hashCache.Contains(hash);
    }

    public void AddHash(string hash)
    {
        _hashCache?.Add(hash);
    }

    // --- Legacy async methods (for backward compatibility) ---

    public async Task<Guid?> GetExamIdAsync(NpgsqlTransaction tx, string name)
    {
        if (_examCache != null) return GetExamId(name);

        using var cmd = new NpgsqlCommand("SELECT id FROM exams WHERE name = @name", await GetConnectionAsync(), tx);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<Guid?> GetStandardIdAsync(NpgsqlTransaction tx, Guid examId, string name)
    {
        if (_standardCache != null) return GetStandardId(examId, name);

        using var cmd = new NpgsqlCommand("SELECT id FROM standards WHERE exam_id = @eid AND name = @name", await GetConnectionAsync(), tx);
        cmd.Parameters.AddWithValue("eid", examId);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<Guid?> GetSubjectIdAsync(NpgsqlTransaction tx, Guid examId, string name)
    {
        if (_subjectCache != null) return GetSubjectId(examId, name);

        using var cmd = new NpgsqlCommand("SELECT id FROM subjects WHERE exam_id = @eid AND name = @name", await GetConnectionAsync(), tx);
        cmd.Parameters.AddWithValue("eid", examId);
        cmd.Parameters.AddWithValue("name", name);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public async Task<Guid?> FindDuplicateAsync(NpgsqlTransaction tx, string contentHash)
    {
        if (_hashCache != null) return IsDuplicateHash(contentHash) ? Guid.NewGuid() : null; // Return dummy for compatibility

        using var cmd = new NpgsqlCommand("SELECT id FROM questions WHERE content_hash = @hash", await GetConnectionAsync(), tx);
        cmd.Parameters.AddWithValue("hash", contentHash);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? null : (Guid?)result;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _conn?.Dispose();
            _isDisposed = true;
        }
    }
}