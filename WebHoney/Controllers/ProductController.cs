using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ProductController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // GET: Product
    public async Task<IActionResult> Index(string? searchString, string? categoryFilter, int page = 1)
    {
        const int pageSize = 5;
        var products = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            products = products.Where(p => 
                p.Name.Contains(searchString) ||
                (p.Description != null && p.Description.Contains(searchString))
            );
        }

        if (!string.IsNullOrEmpty(categoryFilter) && long.TryParse(categoryFilter, out var categoryId))
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        products = products.OrderByDescending(p => p.CreatedAt);

        var totalItems = await products.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedProducts = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["Categories"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        ViewData["SearchString"] = searchString;
        ViewData["CategoryFilter"] = categoryFilter;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedProducts);
    }

    // GET: Product/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }

    // GET: Product/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        return View();
    }

    // Helper method để parse giá tiền từ string (có thể có dấu phẩy hoặc dấu chấm)
    private decimal ParsePrice(string? priceString)
    {
        if (string.IsNullOrWhiteSpace(priceString))
            return 0;

        // Loại bỏ tất cả ký tự không phải số và dấu chấm/phẩy
        var cleaned = priceString.Trim().Replace(" ", "").Replace(",", "").Replace(".", "");
        
        if (decimal.TryParse(cleaned, out decimal result))
            return result;
        
        return 0;
    }

    // POST: Product/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? imageFile, string? Price, [Bind("Name,CategoryId,Description,Origin,Volume,Unit,ImageUrl,Stock")] Product product)
    {
        // Parse giá tiền từ string
        if (!string.IsNullOrWhiteSpace(Price))
        {
            product.Price = ParsePrice(Price);
        }

        // Xóa tất cả lỗi validation liên quan đến CategoryId và Category
        // Cần xóa trước khi kiểm tra để tránh lỗi "The Category field is required"
        ModelState.Remove("CategoryId");
        ModelState.Remove("Category");
        ModelState.Remove("Price"); // Xóa lỗi validation của Price để tự xử lý
        
        // Xóa các lỗi có key chứa "Category" (bao gồm cả "Category.Name", "Category.Id", etc.)
        var keysToRemove = ModelState.Keys.Where(k => k.Contains("Category", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
        
        // Đánh dấu CategoryId là hợp lệ trong ModelState để tránh validation error
        if (ModelState.ContainsKey("CategoryId"))
        {
            ModelState["CategoryId"]!.Errors.Clear();
            ModelState["CategoryId"]!.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
        }
        
        // Kiểm tra CategoryId trước tiên
        if (product.CategoryId == 0 || product.CategoryId <= 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục sản phẩm.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View(product);
        }

        // Kiểm tra giá tiền
        if (product.Price <= 0)
        {
            ModelState.AddModelError("Price", "Giá bán phải lớn hơn 0.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Xử lý upload ảnh nếu có
        if (imageFile != null && imageFile.Length > 0)
        {
            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            try
            {
                // Tạo thư mục uploads/products nếu chưa có
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Lưu URL vào database
                product.ImageUrl = $"/uploads/products/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh sản phẩm");
                ModelState.AddModelError("ImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }
        }
        // Nếu không có file upload và không có ImageUrl, giữ nguyên ImageUrl (có thể null)

        // Xóa lại lỗi CategoryId một lần nữa để đảm bảo
        ModelState.Remove("CategoryId");
        ModelState.Remove("Category");
        var keysToRemove2 = ModelState.Keys.Where(k => k.Contains("Category", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove2)
        {
            ModelState.Remove(key);
        }

        // Kiểm tra lại CategoryId sau khi xử lý ảnh
        if (product.CategoryId == 0 || product.CategoryId <= 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục sản phẩm.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        if (ModelState.IsValid)
        {
            product.Slug = GenerateSlug(product.Name);
            product.IsActive = true;
            product.CreatedAt = DateTime.Now;
            _context.Add(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            TempData["ShowSuccessModal"] = true;
            return RedirectToAction(nameof(Index));
        }
        ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // GET: Product/Edit/5
    public async Task<IActionResult> Edit(long? id, int? page)
    {
        if (id == null) return NotFound();

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        ViewData["ReturnPage"] = page ?? 1; // Lưu trang hiện tại
        return View(product);
    }

    // POST: Product/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, IFormFile? imageFile, string? Price, int? page, [Bind("Id,Name,CategoryId,Description,Origin,Volume,Unit,ImageUrl,Stock,IsActive,CreatedAt")] Product product)
    {
        if (id != product.Id) return NotFound();
        
        var returnPage = page ?? 1; // Lưu trang hiện tại

        // Parse giá tiền từ string
        if (!string.IsNullOrWhiteSpace(Price))
        {
            product.Price = ParsePrice(Price);
        }

        // Xóa tất cả lỗi validation liên quan đến CategoryId và Category
        // Cần xóa trước khi kiểm tra để tránh lỗi "The Category field is required"
        ModelState.Remove("CategoryId");
        ModelState.Remove("Category");
        ModelState.Remove("Price"); // Xóa lỗi validation của Price để tự xử lý
        
        // Xóa các lỗi có key chứa "Category" (bao gồm cả "Category.Name", "Category.Id", etc.)
        var keysToRemove = ModelState.Keys.Where(k => k.Contains("Category", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove)
        {
            ModelState.Remove(key);
        }
        
        // Đánh dấu CategoryId là hợp lệ trong ModelState để tránh validation error
        if (ModelState.ContainsKey("CategoryId"))
        {
            ModelState["CategoryId"]!.Errors.Clear();
            ModelState["CategoryId"]!.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
        }
        
        // Kiểm tra CategoryId trước tiên
        if (product.CategoryId == 0 || product.CategoryId <= 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục sản phẩm.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Kiểm tra giá tiền
        if (product.Price <= 0)
        {
            ModelState.AddModelError("Price", "Giá bán phải lớn hơn 0.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Xử lý upload ảnh nếu có
        if (imageFile != null && imageFile.Length > 0)
        {
            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            try
            {
                // Tạo thư mục uploads/products nếu chưa có
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Lưu URL vào database (ghi đè ImageUrl cũ)
                product.ImageUrl = $"/uploads/products/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh sản phẩm");
                ModelState.AddModelError("ImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }
        }
        // Nếu không có file upload, giữ nguyên ImageUrl hiện tại

        // Đảm bảo CategoryId không có lỗi validation trước khi kiểm tra ModelState.IsValid
        ModelState.Remove("CategoryId");
        ModelState.Remove("Category");
        var keysToRemove2 = ModelState.Keys.Where(k => k.Contains("Category", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var key in keysToRemove2)
        {
            ModelState.Remove(key);
        }
        
        // Đánh dấu CategoryId là hợp lệ
        if (ModelState.ContainsKey("CategoryId"))
        {
            ModelState["CategoryId"]!.Errors.Clear();
            ModelState["CategoryId"]!.ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
        }

        // Kiểm tra lại CategoryId sau khi xử lý ảnh
        if (product.CategoryId == 0 || product.CategoryId <= 0)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục sản phẩm.");
            ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        if (ModelState.IsValid)
        {
            try
            {
                product.Slug = GenerateSlug(product.Name);
                product.UpdatedAt = DateTime.Now;
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                TempData["ShowSuccessModal"] = true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { page = returnPage });
        }
        ViewBag.CategoryId = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        ViewData["CategoryId"] = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Product/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = product.IsActive ? "Kích hoạt sản phẩm thành công!" : "Ẩn sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Product/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }

    // POST: Product/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(long id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("Đ", "d");
    }
}

