using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/admin")]
public class ConceptController : ControllerBase
{
    private readonly IConceptService _conceptService;
    public ConceptController(IConceptService conceptService) => _conceptService = conceptService;

    [HttpGet("topics/{topicId:guid}/concepts")]
    public async Task<IActionResult> GetConcepts(Guid topicId, [FromQuery] bool activeOnly = true) => Ok(await _conceptService.GetConceptsAsync(topicId, activeOnly));

    [HttpPost("topics/{topicId:guid}/concepts")]
    public async Task<IActionResult> CreateConcept(Guid topicId, [FromBody] CreateConceptRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required" });
        try { return CreatedAtAction(nameof(GetConcepts), new { topicId }, await _conceptService.CreateConcept(topicId, req)); }
        catch (MySqlException ex) when (ex.Number == 1062) { return Conflict(new { error = $"Concept '{req.Name}' already exists for this topic" }); }
    }

    [HttpPut("concepts/{id:guid}")]
    public async Task<IActionResult> UpdateConcept(Guid id, [FromBody] UpdateConceptRequest req)
    {
        if (req.Name == null && req.Slug == null && req.Description == null && !req.DisplayOrder.HasValue && !req.IsActive.HasValue) return BadRequest(new { error = "No fields to update" });
        var concept = await _conceptService.UpdateConcept(id, req);
        return concept is null ? NotFound() : Ok(concept);
    }

    [HttpDelete("concepts/{id:guid}")]
    public async Task<IActionResult> DeleteConcept(Guid id) => await _conceptService.DeleteConcept(id) ? NoContent() : NotFound();
}
