using ServiceLayer.Dtos;

namespace ServiceLayer.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task<RegisterResult> RegisterAsync(string username, string email, string password, string fullName);
    Task EnsureSeedUsersAsync();
}
