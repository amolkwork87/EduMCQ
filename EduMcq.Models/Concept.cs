public class Concept
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public record CreateConceptRequest(string Name, string? Slug = null, string? Description = null, short? DisplayOrder = null, bool? IsActive = null);
public record UpdateConceptRequest(string? Name = null, string? Slug = null, string? Description = null, short? DisplayOrder = null, bool? IsActive = null);