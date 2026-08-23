namespace EduMcq.Models;
// Models/ArticleDtos.cs


public class ArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "Admin";
    public string? AuthorAvatar { get; set; }
    public string Category { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? CoverImageUrl { get; set; }
    public short ReadTimeMinutes { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "draft";
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ArticleListResponse
{
    public List<ArticleDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class CreateUpdateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "Admin";
    public string? AuthorAvatar { get; set; }
    public string Category { get; set; } = string.Empty;
    public string[]? Tags { get; set; }
    public string? CoverImageUrl { get; set; }
    public short ReadTimeMinutes { get; set; } = 5;
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "draft";   // draft | published | archived
}