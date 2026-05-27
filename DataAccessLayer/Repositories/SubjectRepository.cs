using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using MongoDB.Driver;

namespace DataAccessLayer.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly MongoDbContext _context;
    public SubjectRepository(MongoDbContext context) => _context = context;

    public Task<List<Subject>> GetAllAsync()
        => _context.Subjects.Find(_ => true).SortBy(s => s.Code).ToListAsync();

    public Task<Subject?> GetByIdAsync(string id)
        => _context.Subjects.Find(s => s.Id == id).FirstOrDefaultAsync()!;

    public Task CreateAsync(Subject subject) => _context.Subjects.InsertOneAsync(subject);

    public Task<long> CountAsync() => _context.Subjects.CountDocumentsAsync(_ => true);
}
