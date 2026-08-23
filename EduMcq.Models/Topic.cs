namespace EduMcq.Models;
public class Topic
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public short DepthLevel { get; set; } = 1;
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public List<Topic> Children { get; set; } = new();
    public List<Concept> Concepts { get; set; } = new();
}

public record CreateTopicRequest(string Name, string? Slug = null, Guid? ParentId = null, short? DisplayOrder = null, bool? IsActive = null);
public record UpdateTopicRequest(string? Name = null, string? Slug = null, Guid? ParentId = null, short? DisplayOrder = null, bool? IsActive = null);