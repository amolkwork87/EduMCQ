using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly ArticleService _service;

    public ArticlesController(ArticleService service)
    {
        _service = service;
    }

    // ---------- PUBLIC (User) ----------

    /// <summary>List published articles</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublished(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var result = await _service.GetArticlesAsync(
            search, category, status: "published", page: page, pageSize: pageSize);

        return Ok(result);
    }

    /// <summary>Get single published article + increment view</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var article = await _service.GetByIdAsync(id);
        if (article == null || article.Status != "published")
            return NotFound();

        // fire-and-forget view count
        _ = _service.IncrementViewAsync(id);

        return Ok(article);
    }

    [HttpPost("{id:guid}/like")]
    [AllowAnonymous]
    public async Task<IActionResult> Like(Guid id)
    {
        var newCount = await _service.IncrementLikeAsync(id);
        return Ok(new { likeCount = newCount });
    }

    // ---------- ADMIN ----------

    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllAdmin(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetArticlesAsync(
            search, category, status, page: page, pageSize: pageSize);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateUpdateArticleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { message = "Title and Content are required" });

        var userIdClaim = User.FindFirst("uid")?.Value;
        Guid? createdBy = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

        var article = await _service.CreateAsync(req, createdBy);
        return Ok(article);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateUpdateArticleRequest req)
    {
        var article = await _service.UpdateAsync(id, req);
        if (article == null) return NotFound();
        return Ok(article);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return Ok(new { message = "Deleted" });
    }

    [HttpPatch("{id:guid}/featured")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ToggleFeatured(Guid id)
    {
        var ok = await _service.ToggleFeaturedAsync(id);
        if (!ok) return NotFound();
        return Ok(new { message = "Featured status updated" });
    }
}