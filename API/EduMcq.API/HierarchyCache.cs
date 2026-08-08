public class HierarchyCache
{
    public Dictionary<(string Exam, string Class, string Subject), (Guid ExamId, Guid? StandardId, Guid SubjectId)> 
        ExamSubjectCache { get; set; } = new();

    public Dictionary<(Guid SubjectId, string Topic, string? Subtopic), Guid> 
        TopicCache { get; set; } = new();

    public Dictionary<(Guid TopicId, string Concept), Guid> 
        ConceptCache { get; set; } = new();

    public Dictionary<string, Guid> 
        SkillCache { get; set; } = new();
}
