using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using ServiceLayer.Dtos;

namespace ServiceLayer.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    public AuthService(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new LoginResult(false, "Username và password không được trống", null, null, null, null);

        var user = await _userRepo.GetByUsernameAsync(username.Trim());
        if (user is null)
            return new LoginResult(false, "Tài khoản không tồn tại", null, null, null, null);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new LoginResult(false, "Sai mật khẩu", null, null, null, null);

        return new LoginResult(true, null, user.Id, user.Username, user.FullName, user.Role);
    }

    public async Task<RegisterResult> RegisterAsync(string username, string email, string password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new RegisterResult(false, "Username và password bắt buộc");
        if (password.Length < 6)
            return new RegisterResult(false, "Mật khẩu phải ít nhất 6 ký tự");

        var existing = await _userRepo.GetByUsernameAsync(username.Trim());
        if (existing is not null)
            return new RegisterResult(false, "Username đã tồn tại");

        var user = new User
        {
            Username = username.Trim(),
            Email = email?.Trim() ?? string.Empty,
            FullName = fullName?.Trim() ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Student"
        };
        await _userRepo.CreateAsync(user);
        return new RegisterResult(true, null);
    }

    public async Task EnsureSeedUsersAsync()
    {
        var admin = await _userRepo.GetByUsernameAsync("admin");
        if (admin is null)
        {
            await _userRepo.CreateAsync(new User
            {
                Username = "admin",
                Email = "admin@chatbot.local",
                FullName = "Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin"
            });
        }

        var mentor = await _userRepo.GetByUsernameAsync("mentor");
        if (mentor is null)
        {
            await _userRepo.CreateAsync(new User
            {
                Username = "mentor",
                Email = "mentor@chatbot.local",
                FullName = "Giảng viên Demo",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("mentor123"),
                Role = "Mentor"
            });
        }

        var student = await _userRepo.GetByUsernameAsync("student");
        if (student is null)
        {
            await _userRepo.CreateAsync(new User
            {
                Username = "student",
                Email = "student@chatbot.local",
                FullName = "Sinh viên Demo",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123"),
                Role = "Student"
            });
        }
    }
}
