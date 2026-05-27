using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using MongoDB.Driver;

namespace DataAccessLayer.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;
    public UserRepository(MongoDbContext context) => _context = context;

    public Task<User?> GetByUsernameAsync(string username)
        => _context.Users.Find(u => u.Username == username).FirstOrDefaultAsync()!;

    public Task<User?> GetByIdAsync(string id)
        => _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync()!;

    public Task<List<User>> GetAllAsync()
        => _context.Users.Find(_ => true).SortByDescending(u => u.CreatedAt).ToListAsync();

    public Task CreateAsync(User user) => _context.Users.InsertOneAsync(user);

    public Task UpdateAsync(User user)
        => _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

    public Task DeleteAsync(string id) => _context.Users.DeleteOneAsync(u => u.Id == id);

    public Task<long> CountAsync() => _context.Users.CountDocumentsAsync(_ => true);

    public Task<long> CountByRoleAsync(string role)
        => _context.Users.CountDocumentsAsync(u => u.Role == role);
}
