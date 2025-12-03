using System.ComponentModel.DataAnnotations;

namespace WebHoney.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên đăng nhập không được quá 100 ký tự")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Display(Name = "Số nhà, tên đường")]
    public string? Street { get; set; }

    [Display(Name = "Tỉnh/Thành phố")]
    public string? ProvinceCode { get; set; }

    [Display(Name = "Quận/Huyện")]
    public string? DistrictCode { get; set; }

    [Display(Name = "Phường/Xã")]
    public string? WardCode { get; set; }

    [StringLength(255, ErrorMessage = "Địa chỉ không được quá 255 ký tự")]
    [Display(Name = "Địa chỉ đầy đủ")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

