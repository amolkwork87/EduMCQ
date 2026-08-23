// Services/ArticleService.cs
using EduMcq.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System.Data;

public class ArticleService
{
    private readonly string _cs;

    public ArticleService(IConfiguration config)
    {
        _cs = config["EduMcqDatabaseConnection"]!;
    }

    // ------------------------------------------------------------------
    // LIST (Admin + User)
    // ------------------------------------------------------------------
    public async Task<ArticleListResponse> GetArticlesAsync(
        string? search = null,
        string? category = null,
        string? status = null,          // null = all (admin), "published" for user
        bool? featuredOnly = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = new ArticleListResponse();
        var where = new List<string> { "1=1" };
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(title ILIKE @search OR excerpt ILIKE @search OR content ILIKE @search 
                         OR EXISTS (SELECT 1 FROM unnest(tags) t WHERE t ILIKE @search))");
            parameters.Add(new NpgsqlParameter("search", $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            where.Add("category = @category");
            parameters.Add(new NpgsqlParameter("category", category));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Add("status = @status");
            parameters.Add(new NpgsqlParameter("status", status));
        }

        if (featuredOnly == true)
        {
            where.Add("is_featured = TRUE");
        }

        var whereClause = string.Join(" AND ", where);

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        // Total count
        var countSql = $"SELECT COUNT(*) FROM articles WHERE {whereClause}";
        await using (var countCmd = new NpgsqlCommand(countSql, conn))
        {
            countCmd.Parameters.AddRange(parameters.ToArray());
            result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        // Data
        var sql = $@"
            SELECT id, title, slug, excerpt, content,
                   author_name, author_avatar, category, tags,
                   cover_image_url, read_time_minutes, is_featured, status,
                   view_count, like_count, published_at, created_at, updated_at
            FROM articles
            WHERE {whereClause}
            ORDER BY 
                is_featured DESC,
                COALESCE(published_at, created_at) DESC
            LIMIT @pageSize OFFSET @offset";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));

        cmd.Parameters.AddWithValue("pageSize", pageSize);
        cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Items.Add(MapArticle(reader));
        }

        return result;
    }

    // ------------------------------------------------------------------
    // GET BY ID
    // ------------------------------------------------------------------
    public async Task<ArticleDto?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id, title, slug, excerpt, content,
                   author_name, author_avatar, category, tags,
                   cover_image_url, read_time_minutes, is_featured, status,
                   view_count, like_count, published_at, created_at, updated_at
            FROM articles
            WHERE id = @id";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapArticle(reader);

        return null;
    }

    // ------------------------------------------------------------------
    // CREATE
    // ------------------------------------------------------------------
    public async Task<ArticleDto> CreateAsync(CreateUpdateArticleRequest req, Guid? createdBy = null)
    {
        const string sql = @"
            INSERT INTO articles (
                title, excerpt, content, author_name, author_avatar,
                category, tags, cover_image_url, read_time_minutes,
                is_featured, status, published_at, created_by
            ) VALUES (
                @title, @excerpt, @content, @authorName, @authorAvatar,
                @category, @tags, @coverImageUrl, @readTime,
                @isFeatured, @status,
                CASE WHEN @status = 'published' THEN now() ELSE NULL END,
                @createdBy
            )
            RETURNING id, title, slug, excerpt, content,
                      author_name, author_avatar, category, tags,
                      cover_image_url, read_time_minutes, is_featured, status,
                      view_count, like_count, published_at, created_at, updated_at";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        // If setting featured, unset others
        if (req.IsFeatured)
            await UnsetOtherFeaturedAsync(conn);

        await using var cmd = new NpgsqlCommand(sql, conn);
        AddArticleParameters(cmd, req);
        cmd.Parameters.AddWithValue("createdBy", (object?)createdBy ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapArticle(reader);
    }

    // ------------------------------------------------------------------
    // UPDATE
    // ------------------------------------------------------------------
    public async Task<ArticleDto?> UpdateAsync(Guid id, CreateUpdateArticleRequest req)
    {
        const string sql = @"
            UPDATE articles SET
                title = @title,
                excerpt = @excerpt,
                content = @content,
                author_name = @authorName,
                author_avatar = @authorAvatar,
                category = @category,
                tags = @tags,
                cover_image_url = @coverImageUrl,
                read_time_minutes = @readTime,
                is_featured = @isFeatured,
                status = @status,
                published_at = CASE 
                    WHEN @status = 'published' AND published_at IS NULL THEN now()
                    WHEN @status <> 'published' THEN NULL
                    ELSE published_at
                END,
                updated_at = now()
            WHERE id = @id
            RETURNING id, title, slug, excerpt, content,
                      author_name, author_avatar, category, tags,
                      cover_image_url, read_time_minutes, is_featured, status,
                      view_count, like_count, published_at, created_at, updated_at";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        if (req.IsFeatured)
            await UnsetOtherFeaturedAsync(conn, id);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        AddArticleParameters(cmd, req);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapArticle(reader);

        return null;
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM articles WHERE id = @id";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ------------------------------------------------------------------
    // TOGGLE FEATURED
    // ------------------------------------------------------------------
    public async Task<bool> ToggleFeaturedAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        // Get current value
        const string getSql = "SELECT is_featured FROM articles WHERE id = @id";
        await using var getCmd = new NpgsqlCommand(getSql, conn);
        getCmd.Parameters.AddWithValue("id", id);
        var current = await getCmd.ExecuteScalarAsync();
        if (current == null) return false;

        var newValue = !(bool)current;

        if (newValue)
            await UnsetOtherFeaturedAsync(conn, id);

        const string updateSql = @"
            UPDATE articles 
            SET is_featured = @value, updated_at = now()
            WHERE id = @id";

        await using var cmd = new NpgsqlCommand(updateSql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("value", newValue);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ------------------------------------------------------------------
    // INCREMENT VIEW
    // ------------------------------------------------------------------
    public async Task IncrementViewAsync(Guid id)
    {
        const string sql = @"
            UPDATE articles 
            SET view_count = view_count + 1 
            WHERE id = @id AND status = 'published'";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ------------------------------------------------------------------
    // INCREMENT LIKE
    // ------------------------------------------------------------------
    public async Task<int> IncrementLikeAsync(Guid id)
    {
        const string sql = @"
            UPDATE articles 
            SET like_count = like_count + 1 
            WHERE id = @id AND status = 'published'
            RETURNING like_count";

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);

        var result = await cmd.ExecuteScalarAsync();
        return result == null ? 0 : Convert.ToInt32(result);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private async Task UnsetOtherFeaturedAsync(NpgsqlConnection conn, Guid? exceptId = null)
    {
        var sql = exceptId.HasValue
            ? "UPDATE articles SET is_featured = FALSE WHERE is_featured = TRUE AND id <> @exceptId"
            : "UPDATE articles SET is_featured = FALSE WHERE is_featured = TRUE";

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (exceptId.HasValue)
            cmd.Parameters.AddWithValue("exceptId", exceptId.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    private void AddArticleParameters(NpgsqlCommand cmd, CreateUpdateArticleRequest req)
    {
        cmd.Parameters.AddWithValue("title", req.Title);
        cmd.Parameters.AddWithValue("excerpt", (object?)req.Excerpt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("content", req.Content);
        cmd.Parameters.AddWithValue("authorName", req.AuthorName);
        cmd.Parameters.AddWithValue("authorAvatar", (object?)req.AuthorAvatar ?? DBNull.Value);
        cmd.Parameters.AddWithValue("category", req.Category);
        cmd.Parameters.AddWithValue("tags", req.Tags ?? Array.Empty<string>());
        cmd.Parameters.AddWithValue("coverImageUrl", (object?)req.CoverImageUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("readTime", req.ReadTimeMinutes);
        cmd.Parameters.AddWithValue("isFeatured", req.IsFeatured);
        cmd.Parameters.AddWithValue("status", req.Status);
    }

    private ArticleDto MapArticle(NpgsqlDataReader r)
    {
        return new ArticleDto
        {
            Id = r.GetGuid(0),
            Title = r.GetString(1),
            Slug = r.IsDBNull(2) ? null : r.GetString(2),
            Excerpt = r.IsDBNull(3) ? null : r.GetString(3),
            Content = r.GetString(4),
            AuthorName = r.GetString(5),
            AuthorAvatar = r.IsDBNull(6) ? null : r.GetString(6),
            Category = r.GetString(7),
            Tags = r.IsDBNull(8) ? Array.Empty<string>() : (string[])r.GetValue(8),
            CoverImageUrl = r.IsDBNull(9) ? null : r.GetString(9),
            ReadTimeMinutes = r.GetInt16(10),
            IsFeatured = r.GetBoolean(11),
            Status = r.GetString(12),
            ViewCount = r.GetInt32(13),
            LikeCount = r.GetInt32(14),
            PublishedAt = r.IsDBNull(15) ? null : r.GetDateTime(15),
            CreatedAt = r.GetDateTime(16),
            UpdatedAt = r.GetDateTime(17)
        };
    }
}