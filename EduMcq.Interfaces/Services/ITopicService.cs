
using EduMcq.Models;

namespace EduMcq.Interfaces;
public interface ITopicService
{
    Task<List<Topic>> GetTopicsAsync(Guid subjectId, Guid? parentId, bool activeOnly = true);
    Task<List<Topic>> GetTopicTreeAsync(Guid subjectId);
    Task<Topic> CreateTopic(Guid subjectId, CreateTopicRequest req);
    Task<Topic?> UpdateTopic(Guid id, UpdateTopicRequest req);
    Task<bool> DeleteTopic(Guid id);

}