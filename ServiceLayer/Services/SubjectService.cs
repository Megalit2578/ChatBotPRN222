using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace ServiceLayer.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repo;
    public SubjectService(ISubjectRepository repo) => _repo = repo;

    public Task<List<Subject>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Subject?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public Task CreateAsync(string code, string name, string description)
        => _repo.CreateAsync(new Subject { Code = code.Trim(), Name = name.Trim(), Description = description?.Trim() ?? string.Empty });

    public async Task EnsureSeedAsync()
    {
        if (await _repo.CountAsync() > 0) return;
        await _repo.CreateAsync(new Subject { Code = "PRN222", Name = "Advanced Cross Platform Application Programming", Description = "Môn học ASP.NET Core MVC tại FPT University." });
        await _repo.CreateAsync(new Subject { Code = "DBI202", Name = "Introduction to Databases", Description = "Cơ sở dữ liệu quan hệ và SQL." });
    }
}
