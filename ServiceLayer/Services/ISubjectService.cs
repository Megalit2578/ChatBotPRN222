using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public interface ISubjectService
{
    Task<List<Subject>> GetAllAsync();
    Task<Subject?> GetByIdAsync(string id);
    Task CreateAsync(string code, string name, string description);
    Task EnsureSeedAsync();
}
