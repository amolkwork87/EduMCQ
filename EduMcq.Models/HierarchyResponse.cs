namespace EduMcq.Models;
public class HierarchyResponse
{
    public Exam? Exam { get; set; }
    public List<Standard> Standards { get; set; } = new();
    public List<SubjectWithTopics> Subjects { get; set; } = new();
}