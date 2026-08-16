namespace EduMcq.Models;

public class Exam
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public short DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}


public record CreateExamRequest(string Name, string? Slug = null, string? Description = null, string? LogoUrl = null, bool? IsActive = null, short? DisplayOrder = null);
public record UpdateExamRequest(string? Name = null, string? Slug = null, string? Description = null, string? LogoUrl = null, bool? IsActive = null, short? DisplayOrder = null);

public class ExamDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Slug { get; set; } = ""; }
