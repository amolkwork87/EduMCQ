namespace EduMcq.Models;
public class SubjectWithTopics
{
    public Subject Subject { get; set; } = new();
    public List<TopicWithConcepts> Topics { get; set; } = new();
}

public class TopicWithConcepts : Topic
{
    public List<Concept> Concepts { get; set; } = new();
}