using System.ComponentModel.DataAnnotations;

namespace WebHoney.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email hoặc tên đăng nhập là bắt buộc")]
    [Display(Name = "Email hoặc Tên đăng nhập")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

