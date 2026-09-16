using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamController(IExamService examService)
    {
        _examService = examService;
    }

    // ============================================================
    // EXAMS
    // ============================================================
    [HttpGet("exams")]
    public async Task<IActionResult> GetExams([FromQuery] bool activeOnly = true)
    {
        return Ok(await _examService.GetExamsAsync(activeOnly));
    }

    [HttpGet("exams/{id:guid}")]
    public async Task<IActionResult> GetExam(Guid id)
    {
        var exam = await _examService.GetExamByIdAsync(id);
        return exam is null ? NotFound(new { error = "Exam not found" }) : Ok(exam);
    }

    [HttpPost("exams")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required" });

        var exam = await _examService.CreateExam(req);
        return CreatedAtAction(nameof(GetExam), new { id = exam.Id }, exam);
    }

    [HttpPut("exams/{id:guid}")]
    public async Task<IActionResult> UpdateExam(Guid id, [FromBody] UpdateExamRequest req)
    {
        if (req.Name == null && req.Slug == null && req.Description == null &&
            req.LogoUrl == null && req.IsActive == null && req.DisplayOrder == null)
            return BadRequest(new { error = "No fields to update" });

        var exam = await _examService.UpdateExam(id, req);
        return exam is null ? NotFound(new { error = "Exam not found" }) : Ok(exam);
    }

    [HttpDelete("exams/{id:guid}")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        return await _examService.DeleteExam(id) ? NoContent() : NotFound();
    }

    
    

}

