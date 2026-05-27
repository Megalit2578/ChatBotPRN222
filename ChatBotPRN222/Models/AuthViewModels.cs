using System.ComponentModel.DataAnnotations;

namespace ChatBotPRN222.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập username")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập username")]
    [MinLength(3)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
