using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.ViewModels;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerController> _logger;
    private readonly IConfiguration _configuration;

    public CustomerController(ApplicationDbContext context, ILogger<CustomerController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // GET: Customer - Danh sách khách hàng (chỉ hiển thị khách hàng đã đăng ký tài khoản)
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 5;
        // Chỉ lấy khách hàng có UserId (tức là đã đăng ký tài khoản)
        var customers = _context.Customers
            .Include(c => c.User)
            .Where(c => c.UserId != null) // Chỉ lấy khách hàng có tài khoản
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            customers = customers.Where(c => 
                c.FullName.Contains(searchString) ||
                (c.Email != null && c.Email.Contains(searchString)) ||
                c.Phone.Contains(searchString) ||
                (c.User != null && c.User.Email.Contains(searchString))
            );
        }

        customers = customers.OrderByDescending(c => c.CreatedAt);

        var totalItems = await customers.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedCustomers = await customers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["SearchString"] = searchString;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedCustomers);
    }

    // GET: Customer/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: Customer/Create
    public IActionResult Create()
    {
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View();
    }

    // POST: Customer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Email,Phone,Address")] Customer customer, string? password)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra email đã tồn tại trong Users chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == customer.Email);

            User? user = null;
            
            if (existingUser != null)
            {
                // Nếu User đã tồn tại, sử dụng User đó
                user = existingUser;
            }
            else
            {
                // Tạo User mới với password mặc định nếu không có password
                var defaultPassword = string.IsNullOrWhiteSpace(password) ? "123456" : password.Trim();
                
                // Tạo username từ email (lấy phần trước @)
                var username = customer.Email?.Split('@')[0] ?? customer.FullName.Replace(" ", "").ToLower();
                
                // Đảm bảo username không trùng
                var usernameBase = username;
                var counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{usernameBase}{counter}";
                    counter++;
                }

                user = new User
                {
                    Username = username,
                    Email = customer.Email ?? "",
                    PasswordHash = defaultPassword, // Lưu plain text theo yêu cầu
                    FullName = customer.FullName,
                    Phone = customer.Phone,
                    Role = "CUSTOMER",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Link Customer với User
            customer.UserId = user.Id;
            customer.CreatedAt = DateTime.Now;
            _context.Add(customer);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Thêm khách hàng thành công! Tài khoản đăng nhập: {user.Email} / Mật khẩu: {(string.IsNullOrWhiteSpace(password) ? "123456" : "đã thiết lập")}";
            return RedirectToAction(nameof(Index));
        }
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View(customer);
    }

    // GET: Customer/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View(customer);
    }

    // POST: Customer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Id,FullName,Email,Phone,Address,CreatedAt")] Customer customer, 
        string? street, string? provinceCode, string? districtCode, string? wardCode)
    {
        if (id != customer.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Xây dựng địa chỉ đầy đủ từ các phần
                var fullAddress = BuildFullAddress(street, wardCode, districtCode, provinceCode);
                if (!string.IsNullOrWhiteSpace(fullAddress))
                {
                    customer.Address = fullAddress;
                }

                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View(customer);
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

    // GET: Customer/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: Customer/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Customer/LockAccount/5 - Form khóa tài khoản
    public async Task<IActionResult> LockAccount(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        if (!customer.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "Khách hàng này chưa có tài khoản đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        // Query User trực tiếp để đảm bảo đúng User
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == customer.UserId.Value);

        if (user == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy tài khoản với ID {customer.UserId}.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new LockAccountViewModel
        {
            CustomerId = id.Value,
            CustomerName = customer.FullName,
            CustomerEmail = customer.Email ?? user.Email
        };
        return View(viewModel);
    }

    // POST: Customer/LockAccount/5 - Khóa tài khoản với lý do
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockAccount(long id, string lockReason)
    {
        // Lấy Customer trước
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        // Kiểm tra Customer có UserId không
        if (!customer.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "Khách hàng này chưa có tài khoản đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(lockReason))
        {
            ModelState.AddModelError("lockReason", "Vui lòng nhập lý do khóa tài khoản.");
            var viewModel = new LockAccountViewModel
            {
                CustomerId = id,
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email ?? ""
            };
            return View(viewModel);
        }

        // Query User trực tiếp bằng UserId của Customer để đảm bảo đúng User
        var userToLock = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == customer.UserId.Value);

        if (userToLock == null)
        {
            TempData["ErrorMessage"] = $"Không tìm thấy tài khoản với ID {customer.UserId}.";
            _logger.LogError($"User not found: UserId={customer.UserId} for Customer ID={customer.Id}, Customer Name={customer.FullName}");
            return RedirectToAction(nameof(Index));
        }

        // Log thông tin để debug
        _logger.LogInformation($"LockAccount - Customer ID: {customer.Id}, Customer Name: {customer.FullName}, Customer UserId: {customer.UserId}, User ID: {userToLock.Id}, User Email: {userToLock.Email}, User Role: {userToLock.Role}");

        // Kiểm tra User có phải là ADMIN không - không cho phép khóa ADMIN
        if (userToLock.Role == "ADMIN" || userToLock.Role == "Admin")
        {
            TempData["ErrorMessage"] = $"Không thể khóa tài khoản quản trị viên. User ID: {userToLock.Id}, Email: {userToLock.Email}";
            _logger.LogWarning($"Attempt to lock ADMIN account: Customer ID={customer.Id}, Customer Name={customer.FullName}, User ID={userToLock.Id}, Email={userToLock.Email}");
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra thêm: User phải có role CUSTOMER
        if (userToLock.Role != "CUSTOMER" && userToLock.Role != "Customer")
        {
            TempData["ErrorMessage"] = $"Không thể khóa tài khoản với role '{userToLock.Role}'. Chỉ có thể khóa tài khoản CUSTOMER.";
            _logger.LogWarning($"Attempt to lock non-CUSTOMER account: Customer ID={customer.Id}, Customer Name={customer.FullName}, User ID={userToLock.Id}, Role={userToLock.Role}");
            return RedirectToAction(nameof(Index));
        }

        // Xác nhận lại: User ID phải khớp với Customer.UserId
        if (userToLock.Id != customer.UserId.Value)
        {
            TempData["ErrorMessage"] = $"Lỗi: Customer.UserId ({customer.UserId}) không khớp với User.Id ({userToLock.Id}). Vui lòng kiểm tra lại dữ liệu trong database.";
            _logger.LogError($"Customer {customer.Id} (Name: {customer.FullName}) có UserId={customer.UserId} nhưng User.Id={userToLock.Id}, User.Email={userToLock.Email}, User.Role={userToLock.Role}");
            return RedirectToAction(nameof(Index));
        }

        // Khóa tài khoản User
        userToLock.IsActive = false;
        userToLock.LockReason = lockReason.Trim();
        userToLock.UpdatedAt = DateTime.Now;

        // Khóa Customer
        customer.IsActive = false;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Successfully locked: Customer ID={customer.Id}, Customer Name={customer.FullName}, User ID={userToLock.Id}, User Email={userToLock.Email}, User Role={userToLock.Role}");
        TempData["SuccessMessage"] = $"Đã khóa tài khoản của khách hàng {customer.FullName} (Email: {userToLock.Email}) thành công!";
        
        return RedirectToAction(nameof(Index));
    }

    // POST: Customer/UnlockAccount/5 - Mở khóa tài khoản
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockAccount(long id)
    {
        var customer = await _context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        if (customer.User != null)
        {
            customer.User.IsActive = true;
            customer.User.LockReason = null;
            customer.User.UpdatedAt = DateTime.Now;
        }

        customer.IsActive = true;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = $"Đã mở khóa tài khoản của khách hàng {customer.FullName} thành công!";
        
        return RedirectToAction(nameof(Index));
    }

    // POST: Customer/ToggleActive/5 - Khóa/Mở khóa khách hàng (giữ lại để tương thích)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var customer = await _context.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        // Nếu đang khóa và có User, chuyển đến form nhập lý do
        if (customer.IsActive && customer.User != null)
        {
            return RedirectToAction(nameof(LockAccount), new { id = id });
        }

        // Nếu đang mở khóa
        if (!customer.IsActive)
        {
            return await UnlockAccount(id);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Customer/CreateUsersForCustomers - Tạo User cho các Customer chưa có User
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUsersForCustomers()
    {
        var customersWithoutUsers = await _context.Customers
            .Where(c => c.UserId == null && !string.IsNullOrEmpty(c.Email))
            .ToListAsync();

        int createdCount = 0;
        int linkedCount = 0;

        foreach (var customer in customersWithoutUsers)
        {
            // Kiểm tra xem đã có User với email này chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == customer.Email);

            User? user = null;

            if (existingUser != null)
            {
                // Nếu User đã tồn tại, link Customer với User đó
                customer.UserId = existingUser.Id;
                linkedCount++;
            }
            else
            {
                // Tạo User mới
                var username = customer.Email!.Split('@')[0];
                var usernameBase = username;
                var counter = 1;
                while (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{usernameBase}{counter}";
                    counter++;
                }

                user = new User
                {
                    Username = username,
                    Email = customer.Email,
                    PasswordHash = "123456", // Mật khẩu mặc định
                    FullName = customer.FullName,
                    Phone = customer.Phone,
                    Role = "CUSTOMER",
                    IsActive = customer.IsActive,
                    CreatedAt = customer.CreatedAt
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                customer.UserId = user.Id;
                createdCount++;
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã tạo {createdCount} tài khoản mới và liên kết {linkedCount} tài khoản hiện có cho khách hàng!";
        return RedirectToAction(nameof(Index));
    }

    private bool CustomerExists(long id)
    {
        return _context.Customers.Any(e => e.Id == id);
    }
}

