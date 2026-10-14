// Controllers/MockTestController.cs
using Microsoft.AspNetCore.Mvc;

using EduMcq.Interfaces;
using EduMcq.Models;
using EduMcq.Repository;

namespace EduMcq.API.Controllers;


[ApiController]
[Route("api/admin/mock-tests")]
public class MockTestController : ControllerBase
{
    private readonly MockTestRepository _repo;
    public MockTestController(MockTestRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var t = await _repo.GetByIdAsync(id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MockTestTemplateRequest req)
    {
        // TODO: replace Guid.Empty with the logged-in user id from claims
        var id = await _repo.CreateAsync(req, null);
        return Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MockTestTemplateRequest req)
        => await _repo.UpdateAsync(id, req) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _repo.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> Toggle(Guid id)
        => await _repo.ToggleActiveAsync(id) ? NoContent() : NotFound();

    // ----- Question bank filtering with usage info -----
    [HttpPost("questions/filter")]
    public async Task<IActionResult> FilterQuestions([FromBody] QuestionFilterRequest req)
        => Ok(await _repo.FilterQuestionsAsync(req));

    // ----- Lookups -----
    [HttpGet("lookups/exams")]
    public async Task<IActionResult> GetExams()
    {
       return Ok(await _repo.GetExamsAsync());
        
    } 

    [HttpGet("lookups/standards")]
    public async Task<IActionResult> GetStandards([FromQuery] Guid? examId)
        => Ok(await _repo.GetStandardsAsync(examId));

    [HttpGet("lookups/subjects")]
    public async Task<IActionResult> GetSubjects() => Ok(await _repo.GetSubjectsAsync());

    [HttpGet("lookups/topics")]
    public async Task<IActionResult> GetTopics([FromQuery] Guid? subjectId)
        => Ok(await _repo.GetTopicsAsync(subjectId));
}