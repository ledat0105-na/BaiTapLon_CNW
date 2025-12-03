using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.Services;
using WebHoney.ViewModels;
using AuthService = WebHoney.Services.IAuthenticationService;

namespace WebHoney.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(AuthService authService, ApplicationDbContext context, ILogger<AccountController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(
            model.Username,
            model.Email,
            model.Password,
            model.FullName,
            model.Phone,
            "Customer"
        );

        if (!result)
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc email đã tồn tại!");
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
            return View(model);
        }

        // Xây dựng địa chỉ đầy đủ từ các phần
        var fullAddress = BuildFullAddress(model.Street, model.WardCode, model.DistrictCode, model.ProvinceCode);
        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            fullAddress = model.Address; // Fallback về địa chỉ cũ nếu có
        }

        // Nếu có địa chỉ, tạo Customer record
        if (!string.IsNullOrWhiteSpace(fullAddress))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user != null)
            {
                var customer = new Customer
                {
                    UserId = user.Id,
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone ?? "",
                    Address = fullAddress,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    // GET: Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authService.LoginAsync(model.UsernameOrEmail, model.Password);

        if (user == null)
        {
            ModelState.AddModelError("", "Tên đăng nhập/email hoặc mật khẩu không đúng!");
            return View(model);
        }

        // Lưu thông tin user vào session
        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("Username", user.Username ?? user.Email);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role);

        if (model.RememberMe)
        {
            HttpContext.Session.SetString("RememberMe", "true");
        }

        _logger.LogInformation($"User {user.Username} logged in successfully");

        // Redirect về trang đã yêu cầu nếu có
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Phân quyền redirect theo Role (database dùng "ADMIN" hoặc "CUSTOMER")
        if (user.Role == "ADMIN" || user.Role == "Admin")
        {
            return RedirectToAction("Index", "Admin");
        }
        
        // Customer hoặc các role khác -> về trang chủ
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["SuccessMessage"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied()
    {
        return View();
    }

    private string? BuildFullAddress(string? street, string? wardCode, string? districtCode, string? provinceCode)
    {
        // Địa chỉ đầy đủ sẽ được JavaScript tự động cập nhật vào trường Address
        // Method này chỉ là fallback nếu cần
        if (string.IsNullOrWhiteSpace(street) && 
            string.IsNullOrWhiteSpace(wardCode) && 
            string.IsNullOrWhiteSpace(districtCode) && 
            string.IsNullOrWhiteSpace(provinceCode))
        {
            return null;
        }

        // Trường hợp đơn giản: nếu có street thì trả về street
        // Thực tế địa chỉ đầy đủ sẽ được JavaScript cập nhật
        return street;
    }
}

