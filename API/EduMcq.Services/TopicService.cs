using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class TopicService : ITopicService
{
    private readonly ITopicRepository _repository;
    public TopicService(ITopicRepository repository) => _repository = repository;
    public Task<Topic> CreateTopic(Guid subjectId, CreateTopicRequest req) => _repository.CreateTopic(subjectId, req);
    public Task<bool> DeleteTopic(Guid id) => _repository.DeleteTopic(id);
    public Task<List<Topic>> GetTopicsAsync(Guid subjectId, Guid? parentId, bool activeOnly = true) => _repository.GetTopicsAsync(subjectId, parentId, activeOnly);
    public Task<List<Topic>> GetTopicTreeAsync(Guid subjectId) => _repository.GetTopicTreeAsync(subjectId);
    public Task<Topic?> UpdateTopic(Guid id, UpdateTopicRequest req) => _repository.UpdateTopic(id, req);
}
