using EduMcq.Models;

namespace EduMcq.Interfaces;

public interface IArticleRepository
{
    Task<ArticleListResponse> GetArticlesAsync(
        string? search = null,
        string? category = null,
        string? status = null,
        bool? featuredOnly = null,
        int page = 1,
        int pageSize = 20);

    Task<ArticleDto?> GetByIdAsync(Guid id);
    Task<ArticleDto> CreateAsync(CreateUpdateArticleRequest req, Guid? createdBy = null);
    Task<ArticleDto?> UpdateAsync(Guid id, CreateUpdateArticleRequest req);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ToggleFeaturedAsync(Guid id);
    Task IncrementViewAsync(Guid id);
    Task<int> IncrementLikeAsync(Guid id);
}
