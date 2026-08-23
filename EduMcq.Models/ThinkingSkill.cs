namespace EduMcq.Models;
public class ThinkingSkill
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "cognitive";
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public record CreateThinkingSkillRequest(string Name, string? Slug = null, string? Description = null, string? Category = null, short? DisplayOrder = null, bool? IsActive = null);
public record UpdateThinkingSkillRequest(string? Name = null, string? Slug = null, string? Description = null, string? Category = null, short? DisplayOrder = null, bool? IsActive = null);