using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.Extensions;
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
    private readonly ICartService _cartService;
    private readonly IWebHostEnvironment _env;

    public AccountController(AuthService authService, ApplicationDbContext context, ILogger<AccountController> logger, IConfiguration configuration, ICartService cartService, IWebHostEnvironment env)
    {
        _authService = authService;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _cartService = cartService;
        _env = env;
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

        var loginResult = await _authService.LoginWithResultAsync(model.UsernameOrEmail, model.Password);

        if (!loginResult.Success)
        {
            if (!string.IsNullOrEmpty(loginResult.LockReason))
            {
                // Tài khoản bị khóa - lưu lý do vào TempData và ViewData để hiển thị modal
                TempData["AccountLocked"] = true;
                TempData["LockReason"] = loginResult.LockReason;
                ViewData["AccountLocked"] = true;
                ViewData["LockReason"] = loginResult.LockReason;
                return View(model);
            }
            
            ModelState.AddModelError("", loginResult.ErrorMessage ?? "Tên đăng nhập/email hoặc mật khẩu không đúng!");
            return View(model);
        }

        var user = loginResult.User;
        if (user == null)
        {
            ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng thử lại!");
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

        // Cập nhật thời điểm đăng nhập gần nhất
        var userEntity = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.UserId);
        if (userEntity != null)
        {
            userEntity.LastLoginAt = DateTime.Now;
            userEntity.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation($"User {user.Username} logged in successfully");

        // Mỗi lần đăng nhập: luôn thay thế giỏ hàng trong Session bằng giỏ hàng đã lưu trong DB
        // -> tránh cộng dồn số lượng khi trước đó đã có giỏ hàng guest hoặc do các lần login trước.
        _cartService.ClearCart(HttpContext.Session);

        var savedCartItems = await _context.UserCartItems
            .Where(ci => ci.UserId == user.UserId)
            .ToListAsync();

        foreach (var item in savedCartItems)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive && p.Stock > 0);

            if (product != null && item.Quantity > 0)
            {
                _cartService.AddToCart(HttpContext.Session, item.ProductId, item.Quantity, product);
            }
        }

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

    // GET: Account/Profile - Trang cá nhân khách hàng
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", new { returnUrl = Url.Action("Profile", "Account") });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            // Nếu user không tồn tại nữa thì cho đăng xuất
            return RedirectToAction("Logout");
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == userId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new AccountProfileViewModel
        {
            User = user,
            Orders = orders
        };

        return View(vm);
    }

    // POST: Account/UploadAvatar - Upload ảnh đại diện
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return Json(new { success = false, message = "Không tìm thấy tài khoản." });
        }

        if (avatarFile == null || avatarFile.Length == 0)
        {
            return Json(new { success = false, message = "Vui lòng chọn ảnh." });
        }

        // Kiểm tra định dạng ảnh
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp)." });
        }

        // Kiểm tra kích thước file (max 5MB)
        if (avatarFile.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "Kích thước file không được vượt quá 5MB." });
        }

        try
        {
            // Tạo thư mục uploads/avatars nếu chưa có
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Xóa ảnh cũ nếu có
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Tạo tên file unique
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            // Cập nhật URL vào database
            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = user.AvatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi upload avatar");
            return Json(new { success = false, message = "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại." });
        }
    }

    // GET: Account/Logout
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.Session.GetUserId();

        // Lưu giỏ hàng hiện tại xuống database gắn với tài khoản
        if (userId.HasValue)
        {
            var cart = _cartService.GetCart(HttpContext.Session);

            // Xóa các item cũ của user trong DB
            var existingItems = _context.UserCartItems.Where(ci => ci.UserId == userId.Value);
            _context.UserCartItems.RemoveRange(existingItems);

            // Thêm lại theo giỏ hiện tại
            foreach (var item in cart.Values)
            {
                if (item.Quantity <= 0) continue;

                _context.UserCartItems.Add(new UserCartItem
                {
                    UserId = userId.Value,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        // Xóa thông tin đăng nhập và giỏ hàng trong Session
        HttpContext.Session.Remove("UserId");
        HttpContext.Session.Remove("Username");
        HttpContext.Session.Remove("Email");
        HttpContext.Session.Remove("FullName");
        HttpContext.Session.Remove("Role");
        HttpContext.Session.Remove("RememberMe");
        HttpContext.Session.Remove("Cart");

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

