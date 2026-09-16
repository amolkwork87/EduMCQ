using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class StandardController : ControllerBase
{
    private readonly IStandardService _standardService;
    public StandardController(IStandardService standardService) => _standardService = standardService;

    [HttpGet("exams/{examId:guid}/standards")]
    public async Task<IActionResult> GetStandards(Guid examId, [FromQuery] bool activeOnly = true) => Ok(await _standardService.GetStandardsAsync(examId, activeOnly));

    [HttpPost("exams/{examId:guid}/standards")]
    public async Task<IActionResult> CreateStandard(Guid examId, [FromBody] CreateStandardRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required" });
        try { return CreatedAtAction(nameof(GetStandards), new { examId }, await _standardService.CreateStandard(examId, req)); }
        catch (PostgresException ex) when (ex.SqlState == "23505") { return Conflict(new { error = $"Standard '{req.Name}' already exists for this exam" }); }
    }

    [HttpPut("standards/{id:guid}")]
    public async Task<IActionResult> UpdateStandard(Guid id, [FromBody] UpdateStandardRequest req)
    {
        if (req.Name == null && req.Slug == null && !req.DisplayOrder.HasValue && !req.IsActive.HasValue) return BadRequest(new { error = "No fields to update" });
        var standard = await _standardService.UpdateStandard(id, req);
        return standard is null ? NotFound() : Ok(standard);
    }

    [HttpDelete("standards/{id:guid}")]
    public async Task<IActionResult> DeleteStandard(Guid id) => await _standardService.DeleteStandard(id) ? NoContent() : NotFound();
}
