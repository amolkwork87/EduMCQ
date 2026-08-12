namespace EduMcq.Models;
public class Subject
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public record CreateSubjectRequest(string Name, string? Slug = null, short? DisplayOrder = null, bool? IsActive = null);
public record UpdateSubjectRequest(string? Name = null, string? Slug = null, short? DisplayOrder = null, bool? IsActive = null);