namespace EduMcq.Models.BulkUpload;

public record PreparedQuestion(
    QuestionRow Row,
    Guid QuestionId,
    string Hash,
    Guid ExamId,
    Guid StandardId,
    Guid SubjectId,
    Guid? TopicId,
    Guid? ConceptId);