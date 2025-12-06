using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Services;

public interface IAuthenticationService
{
    Task<bool> RegisterAsync(string username, string email, string password, string fullName, string? phone, string role = "Customer");
    Task<User?> LoginAsync(string usernameOrEmail, string password);
    Task<LoginResult> LoginWithResultAsync(string usernameOrEmail, string password);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class LoginResult
{
    public bool Success { get; set; }
    public User? User { get; set; }
    public string? LockReason { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationService>? _logger;

    public AuthenticationService(ApplicationDbContext context, ILogger<AuthenticationService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(string username, string email, string password, string fullName, string? phone, string role = "CUSTOMER")
    {
        // Kiểm tra email hoặc username đã tồn tại chưa
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email || (u.Username != null && u.Username == username));

        if (existingUser != null)
        {
            return false; // User đã tồn tại
        }

        // Tạo user mới - lưu password trực tiếp không hash (theo yêu cầu)
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = password, // Lưu trực tiếp không hash
            FullName = fullName,
            Phone = phone,
            Role = role.ToUpper(), // ADMIN hoặc CUSTOMER
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> LoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            _logger?.LogWarning("Login attempt with empty username/email or password");
            return null;
        }

        var searchTerm = usernameOrEmail.Trim().ToLower();
        var passwordToCheck = password.Trim();

        // Tìm user theo username hoặc email (không phân biệt hoa thường)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Username != null && u.Username.ToLower() == searchTerm) || 
                u.Email.ToLower() == searchTerm);

        if (user == null)
        {
            _logger?.LogWarning($"User not found: {usernameOrEmail}");
            return null;
        }

        if (!user.IsActive)
        {
            _logger?.LogWarning($"User is inactive: {usernameOrEmail}");
            return null;
        }

        // Verify password - so sánh trực tiếp không hash (vì DB đã lưu plain text)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger?.LogWarning($"User has no password hash: {usernameOrEmail}");
            return null;
        }

        if (!VerifyPassword(passwordToCheck, user.PasswordHash))
        {
            _logger?.LogWarning($"Password mismatch for user: {usernameOrEmail}");
            return null;
        }

        // Cập nhật updated_at (thay vì last_login)
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        _logger?.LogInformation($"User logged in successfully: {usernameOrEmail}");
        return user;
    }

    public async Task<LoginResult> LoginWithResultAsync(string usernameOrEmail, string password)
    {
        var result = new LoginResult();
        
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            result.Success = false;
            result.ErrorMessage = "Tên đăng nhập/email hoặc mật khẩu không được để trống";
            return result;
        }

        var searchTerm = usernameOrEmail.Trim().ToLower();
        var passwordToCheck = password.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Username != null && u.Username.ToLower() == searchTerm) || 
                u.Email.ToLower() == searchTerm);

        if (user == null)
        {
            result.Success = false;
            result.ErrorMessage = "Tên đăng nhập/email hoặc mật khẩu không đúng";
            return result;
        }

        if (!user.IsActive)
        {
            result.Success = false;
            result.LockReason = user.LockReason ?? "Tài khoản của bạn đã bị khóa";
            return result;
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            result.Success = false;
            result.ErrorMessage = "Tài khoản không có mật khẩu";
            return result;
        }

        if (!VerifyPassword(passwordToCheck, user.PasswordHash))
        {
            result.Success = false;
            result.ErrorMessage = "Tên đăng nhập/email hoặc mật khẩu không đúng";
            return result;
        }

        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        result.Success = true;
        result.User = user;
        return result;
    }

    public string HashPassword(string password)
    {
        // Không hash - trả về password trực tiếp
        return password;
    }

    public bool VerifyPassword(string password, string? hash)
    {
        // So sánh trực tiếp không hash (vì DB lưu plain text)
        if (string.IsNullOrEmpty(hash)) return false;
        return password == hash;
    }
}

