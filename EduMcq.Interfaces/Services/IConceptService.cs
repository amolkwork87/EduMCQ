
using EduMcq.Models;

namespace EduMcq.Interfaces;
public interface IConceptService
{
    Task<List<Concept>> GetConceptsAsync(Guid topicId, bool activeOnly = true);
    Task<Concept> CreateConcept(Guid topicId, CreateConceptRequest req);
    Task<Concept?> UpdateConcept(Guid id, UpdateConceptRequest req);
    Task<bool> DeleteConcept(Guid id);

}