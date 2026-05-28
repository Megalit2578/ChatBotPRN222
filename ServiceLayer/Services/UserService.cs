using DataAccessLayer.Constants;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace ServiceLayer.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;

    public Task<List<User>> GetAllAsync() => _repo.GetAllAsync();
    public Task<User?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public async Task<(bool, string?)> CreateAsync(string username, string email, string fullName, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username và mật khẩu bắt buộc");
        if (password.Length < 6)
            return (false, "Mật khẩu tối thiểu 6 ký tự");
        if (!Roles.All.Contains(role))
            return (false, "Role không hợp lệ");
        if (await _repo.GetByUsernameAsync(username.Trim()) is not null)
            return (false, "Username đã tồn tại");

        await _repo.CreateAsync(new User
        {
            Username = username.Trim(),
            Email = email?.Trim() ?? string.Empty,
            FullName = fullName?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        });
        return (true, null);
    }

    public async Task<(bool, string?)> UpdateRoleAsync(string id, string newRole)
    {
        if (!Roles.All.Contains(newRole)) return (false, "Role không hợp lệ");
        var user = await _repo.GetByIdAsync(id);
        if (user is null) return (false, "User không tồn tại");
        user.Role = newRole;
        await _repo.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool, string?)> ResetPasswordAsync(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Mật khẩu tối thiểu 6 ký tự");
        var user = await _repo.GetByIdAsync(id);
        if (user is null) return (false, "User không tồn tại");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _repo.UpdateAsync(user);
        return (true, null);
    }

    public async Task<(bool, string?)> DeleteAsync(string id)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user is null) return (false, "User không tồn tại");
        if (user.Role == Roles.Admin && await _repo.CountByRoleAsync(Roles.Admin) <= 1)
            return (false, "Không thể xoá Admin cuối cùng");
        await _repo.DeleteAsync(id);
        return (true, null);
    }

    public async Task<(long, long, long, long)> GetCountsAsync()
    {
        var total = await _repo.CountAsync();
        var admins = await _repo.CountByRoleAsync(Roles.Admin);
        var lecturers = await _repo.CountByRoleAsync(Roles.Lecturer);
        var students = await _repo.CountByRoleAsync(Roles.Student);
        return (total, admins, lecturers, students);
    }
}
