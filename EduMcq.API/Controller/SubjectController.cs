using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EduMcq.Controllers;

[ApiController]
[Route("api/admin")]
//[Authorize]
public class SubjectController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    public SubjectController(ISubjectService subjectService) => _subjectService = subjectService;

    [HttpGet("exams/{examId:guid}/subjects")]
    public async Task<IActionResult> GetSubjects(Guid examId, [FromQuery] bool activeOnly = true) => Ok(await _subjectService.GetSubjectsAsync(examId, activeOnly));

    [HttpPost("exams/{examId:guid}/subjects")]
    public async Task<IActionResult> CreateSubject(Guid examId, [FromBody] CreateSubjectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required" });
        try { return CreatedAtAction(nameof(GetSubjects), new { examId }, await _subjectService.CreateSubject(examId, req)); }
        catch (PostgresException ex) when (ex.SqlState == "23505") { return Conflict(new { error = $"Subject '{req.Name}' already exists for this exam" }); }
    }

    [HttpPut("subjects/{id:guid}")]
    public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectRequest req)
    {
        if (req.Name == null && req.Slug == null && !req.DisplayOrder.HasValue && !req.IsActive.HasValue) return BadRequest(new { error = "No fields to update" });
        var subject = await _subjectService.UpdateSubject(id, req);
        return subject is null ? NotFound() : Ok(subject);
    }

    [HttpDelete("subjects/{id:guid}")]
    public async Task<IActionResult> DeleteSubject(Guid id) => await _subjectService.DeleteSubject(id) ? NoContent() : NotFound();
}
