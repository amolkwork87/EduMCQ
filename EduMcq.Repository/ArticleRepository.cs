using EduMcq.Interfaces;
using EduMcq.Models;
using MySqlConnector;
using System.Text.Json;

namespace EduMcq.Repository;

public class ArticleRepository : IArticleRepository
{
    private readonly string _connectionString;

    public ArticleRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task<ArticleListResponse> GetArticlesAsync(
        string? search = null,
        string? category = null,
        string? status = null,
        bool? featuredOnly = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = new ArticleListResponse();
        var where = new List<string> { "1=1" };
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add(@"(LOWER(title) LIKE LOWER(@search) OR LOWER(excerpt) LIKE LOWER(@search)
                          OR LOWER(content) LIKE LOWER(@search)
                          OR JSON_SEARCH(tags, 'one', @search) IS NOT NULL)");
            parameters.Add(new MySqlParameter("search", $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            where.Add("category = @category");
            parameters.Add(new MySqlParameter("category", category));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            where.Add("status = @status");
            parameters.Add(new MySqlParameter("status", status));
        }

        if (featuredOnly == true)
        {
            where.Add("is_featured = TRUE");
        }

        var whereClause = string.Join(" AND ", where);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var countSql = $"SELECT COUNT(*) FROM articles WHERE {whereClause}";
        await using (var countCommand = new MySqlCommand(countSql, connection))
        {
            countCommand.Parameters.AddRange(parameters.ToArray());
            result.TotalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        }

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

        await using var command = new MySqlCommand(sql, connection);
        foreach (var parameter in parameters)
            command.Parameters.Add(new MySqlParameter(parameter.ParameterName, parameter.Value));

        command.Parameters.AddWithValue("pageSize", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Items.Add(MapArticle(reader));

        return result;
    }

    public async Task<ArticleDto?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT id, title, slug, excerpt, content,
                   author_name, author_avatar, category, tags,
                   cover_image_url, read_time_minutes, is_featured, status,
                   view_count, like_count, published_at, created_at, updated_at
            FROM articles
            WHERE id = @id";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapArticle(reader);

        return null;
    }

    public async Task<ArticleDto> CreateAsync(CreateUpdateArticleRequest req, Guid? createdBy = null)
    {
        var id = Guid.NewGuid();
        const string sql = @"
            INSERT INTO articles (
                id, title, excerpt, content, author_name, author_avatar,
                category, tags, cover_image_url, read_time_minutes,
                is_featured, status, published_at, created_by
            ) VALUES (
                @id, @title, @excerpt, @content, @authorName, @authorAvatar,
                @category, @tags, @coverImageUrl, @readTime,
                @isFeatured, @status,
                CASE WHEN @status = 'published' THEN now() ELSE NULL END,
                @createdBy
            )";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        if (req.IsFeatured)
            await UnsetOtherFeaturedAsync(connection);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        AddArticleParameters(command, req);
        command.Parameters.AddWithValue("createdBy", (object?)createdBy ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand(@"SELECT id, title, slug, excerpt, content,
                   author_name, author_avatar, category, tags,
                   cover_image_url, read_time_minutes, is_featured, status,
                   view_count, like_count, published_at, created_at, updated_at
            FROM articles WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapArticle(reader);
    }

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
            WHERE id = @id";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        if (req.IsFeatured)
            await UnsetOtherFeaturedAsync(connection, id);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        AddArticleParameters(command, req);

        await command.ExecuteNonQueryAsync();
        await using var select = new MySqlCommand(@"SELECT id, title, slug, excerpt, content,
                   author_name, author_avatar, category, tags,
                   cover_image_url, read_time_minutes, is_featured, status,
                   view_count, like_count, published_at, created_at, updated_at
            FROM articles WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        await using var reader = await select.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapArticle(reader);

        return null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM articles WHERE id = @id";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ToggleFeaturedAsync(Guid id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string getSql = "SELECT is_featured FROM articles WHERE id = @id";
        await using var getCommand = new MySqlCommand(getSql, connection);
        getCommand.Parameters.AddWithValue("id", id);
        var current = await getCommand.ExecuteScalarAsync();
        if (current == null)
            return false;

        var newValue = !(bool)current;

        if (newValue)
            await UnsetOtherFeaturedAsync(connection, id);

        const string updateSql = @"
            UPDATE articles 
            SET is_featured = @value, updated_at = now()
            WHERE id = @id";

        await using var command = new MySqlCommand(updateSql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("value", newValue);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task IncrementViewAsync(Guid id)
    {
        const string sql = @"
            UPDATE articles 
            SET view_count = view_count + 1 
            WHERE id = @id AND status = 'published'";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> IncrementLikeAsync(Guid id)
    {
        const string sql = @"
            UPDATE articles
            SET like_count = like_count + 1
            WHERE id = @id AND status = 'published'";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        var rows = await command.ExecuteNonQueryAsync();
        if (rows == 0) return 0;
        await using var select = new MySqlCommand("SELECT like_count FROM articles WHERE id = @id", connection);
        select.Parameters.AddWithValue("id", id);
        var result = await select.ExecuteScalarAsync();
        return result == null ? 0 : Convert.ToInt32(result);
    }

    private async Task UnsetOtherFeaturedAsync(MySqlConnection connection, Guid? exceptId = null)
    {
        var sql = exceptId.HasValue
            ? "UPDATE articles SET is_featured = FALSE WHERE is_featured = TRUE AND id <> @exceptId"
            : "UPDATE articles SET is_featured = FALSE WHERE is_featured = TRUE";

        await using var command = new MySqlCommand(sql, connection);
        if (exceptId.HasValue)
            command.Parameters.AddWithValue("exceptId", exceptId.Value);

        await command.ExecuteNonQueryAsync();
    }

    private static void AddArticleParameters(MySqlCommand command, CreateUpdateArticleRequest req)
    {
        command.Parameters.AddWithValue("title", req.Title);
        command.Parameters.AddWithValue("excerpt", (object?)req.Excerpt ?? DBNull.Value);
        command.Parameters.AddWithValue("content", req.Content);
        command.Parameters.AddWithValue("authorName", req.AuthorName);
        command.Parameters.AddWithValue("authorAvatar", (object?)req.AuthorAvatar ?? DBNull.Value);
        command.Parameters.AddWithValue("category", req.Category);
        command.Parameters.AddWithValue("tags", JsonSerializer.Serialize(req.Tags ?? Array.Empty<string>()));
        command.Parameters.AddWithValue("coverImageUrl", (object?)req.CoverImageUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("readTime", req.ReadTimeMinutes);
        command.Parameters.AddWithValue("isFeatured", req.IsFeatured);
        command.Parameters.AddWithValue("status", req.Status);
    }

    private static ArticleDto MapArticle(MySqlDataReader reader)
    {
        return new ArticleDto
        {
            Id = reader.GetGuid(0),
            Title = reader.GetString(1),
            Slug = reader.IsDBNull(2) ? null : reader.GetString(2),
            Excerpt = reader.IsDBNull(3) ? null : reader.GetString(3),
            Content = reader.GetString(4),
            AuthorName = reader.GetString(5),
            AuthorAvatar = reader.IsDBNull(6) ? null : reader.GetString(6),
            Category = reader.GetString(7),
            Tags = reader.IsDBNull(8) ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(reader.GetString(8)) ?? Array.Empty<string>(),
            CoverImageUrl = reader.IsDBNull(9) ? null : reader.GetString(9),
            ReadTimeMinutes = reader.GetInt16(10),
            IsFeatured = reader.GetBoolean(11),
            Status = reader.GetString(12),
            ViewCount = reader.GetInt32(13),
            LikeCount = reader.GetInt32(14),
            PublishedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            CreatedAt = reader.GetDateTime(16),
            UpdatedAt = reader.GetDateTime(17)
        };
    }
}
