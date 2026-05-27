using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<(bool Success, string? Error)> CreateAsync(string username, string email, string fullName, string password, string role);
    Task<(bool Success, string? Error)> UpdateRoleAsync(string id, string newRole);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string id, string newPassword);
    Task<(bool Success, string? Error)> DeleteAsync(string id);
    Task<(long Total, long Admins, long Mentors, long Students)> GetCountsAsync();
}
