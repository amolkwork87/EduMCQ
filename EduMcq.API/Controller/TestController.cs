using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;

    public TestController(ITestService testService)
    {
        _testService = testService;
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams()
    {
        return Ok(await _testService.GetExamsAsync());
    }

    [HttpGet("standards")]
    public async Task<IActionResult> GetStandards([FromQuery] Guid exam_id)
    {
        return Ok(await _testService.GetStandardsAsync(exam_id));
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects([FromQuery] Guid exam_id)
    {
        return Ok(await _testService.GetSubjectsAsync(exam_id));
    }

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions(
        [FromQuery] Guid subject_id,
        [FromQuery] Guid standard_id,
        [FromQuery] int limit = 10)
    {
        return Ok(await _testService.GetQuestionsAsync(subject_id, standard_id, limit));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitTest([FromBody] TestSubmissionDto submission)
    {
        return Ok(await _testService.SubmitTestAsync(submission));
    }

    // ---------- List available mock tests for the student ----------
    // GET /api/test/available?examId=&standardId=&subjectId=
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTests(
        [FromQuery] Guid? examId,
        [FromQuery] Guid? standardId,
        [FromQuery] Guid? subjectId)
    {
        var data = await _testService.GetAvailableTemplatesAsync(examId, standardId, subjectId);
        return Ok(data);
    }

    // ---------- Load a test's questions in randomized order ----------
    // GET /api/test/{templateId}/questions
    [HttpGet("{templateId:guid}/questions")]
    public async Task<IActionResult> GetTestQuestions(Guid templateId)
    {
        var data = await _testService.GetTemplateQuestionsAsync(templateId);
        if (data == null) return NotFound();
        return Ok(data);
    }

    // ---------- Submit answers ----------
    [HttpPost("{templateId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid templateId, [FromBody] SubmissionRequest req)
    {
        var userIdClaim = User.FindFirst("uid")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        req.UserId = userId.ToString();
        var result = await _testService.SubmitAndScoreAsync(templateId, req);
        return Ok(result);
    }
}
