namespace EduMcq.Models;

public class Standard
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public record CreateStandardRequest(string Name, string? Slug = null, short? DisplayOrder = null, bool? IsActive = null);
public record UpdateStandardRequest(string? Name = null, string? Slug = null, short? DisplayOrder = null, bool? IsActive = null);