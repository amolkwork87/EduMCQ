using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class TopicController : ControllerBase
{
    private readonly ITopicService _topicService;
    public TopicController(ITopicService topicService) => _topicService = topicService;

    [HttpGet("subjects/{subjectId:guid}/topics")]
    public async Task<IActionResult> GetTopics(Guid subjectId, [FromQuery] Guid? parentId, [FromQuery] bool activeOnly = true) =>
        Ok(await _topicService.GetTopicsAsync(subjectId, parentId, activeOnly));

    [HttpGet("topics/tree/{subjectId:guid}")]
    public async Task<IActionResult> GetTopicTree(Guid subjectId) => Ok(await _topicService.GetTopicTreeAsync(subjectId));

    [HttpPost("subjects/{subjectId:guid}/topics")]
    public async Task<IActionResult> CreateTopic(Guid subjectId, [FromBody] CreateTopicRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required" });
        try { return CreatedAtAction(nameof(GetTopics), new { subjectId }, await _topicService.CreateTopic(subjectId, req)); }
        catch (PostgresException ex) when (ex.SqlState == "23505") { return Conflict(new { error = $"Topic '{req.Name}' already exists at this level" }); }
    }

    [HttpPut("topics/{id:guid}")]
    public async Task<IActionResult> UpdateTopic(Guid id, [FromBody] UpdateTopicRequest req)
    {
        if (req.Name == null && req.Slug == null && !req.ParentId.HasValue && !req.DisplayOrder.HasValue && !req.IsActive.HasValue) return BadRequest(new { error = "No fields to update" });
        var topic = await _topicService.UpdateTopic(id, req);
        return topic is null ? NotFound() : Ok(topic);
    }

    [HttpDelete("topics/{id:guid}")]
    public async Task<IActionResult> DeleteTopic(Guid id) => await _topicService.DeleteTopic(id) ? NoContent() : NotFound();
}
