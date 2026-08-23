// Services/ArticleService.cs
using EduMcq.Interfaces;
using EduMcq.Models;

public class ArticleService
{
    private readonly IArticleRepository _repository;

    public ArticleService(IArticleRepository repository)
    {
        _repository = repository;
    }

    public Task<ArticleListResponse> GetArticlesAsync(
        string? search = null,
        string? category = null,
        string? status = null,
        bool? featuredOnly = null,
        int page = 1,
        int pageSize = 20)
    {
        return _repository.GetArticlesAsync(search, category, status, featuredOnly, page, pageSize);
    }

    public Task<ArticleDto?> GetByIdAsync(Guid id)
    {
        return _repository.GetByIdAsync(id);
    }

    public Task<ArticleDto> CreateAsync(CreateUpdateArticleRequest req, Guid? createdBy = null)
    {
        return _repository.CreateAsync(req, createdBy);
    }

    public Task<ArticleDto?> UpdateAsync(Guid id, CreateUpdateArticleRequest req)
    {
        return _repository.UpdateAsync(id, req);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _repository.DeleteAsync(id);
    }

    public Task<bool> ToggleFeaturedAsync(Guid id)
    {
        return _repository.ToggleFeaturedAsync(id);
    }

    public Task IncrementViewAsync(Guid id)
    {
        return _repository.IncrementViewAsync(id);
    }

    public Task<int> IncrementLikeAsync(Guid id)
    {
        return _repository.IncrementLikeAsync(id);
    }
}