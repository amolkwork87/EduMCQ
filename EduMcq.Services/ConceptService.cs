using EduMcq.Interfaces;
using EduMcq.Models;

namespace EduMcq.Services;

public class ConceptService : IConceptService
{
    private readonly IConceptRepository _repository;
    public ConceptService(IConceptRepository repository) => _repository = repository;
    public Task<Concept> CreateConcept(Guid topicId, CreateConceptRequest req) => _repository.CreateConcept(topicId, req);
    public Task<bool> DeleteConcept(Guid id) => _repository.DeleteConcept(id);
    public Task<List<Concept>> GetConceptsAsync(Guid topicId, bool activeOnly = true) => _repository.GetConceptsAsync(topicId, activeOnly);
    public Task<Concept?> UpdateConcept(Guid id, UpdateConceptRequest req) => _repository.UpdateConcept(id, req);
}
