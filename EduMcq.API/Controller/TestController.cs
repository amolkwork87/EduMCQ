using EduMcq.Interfaces;
using EduMcq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
