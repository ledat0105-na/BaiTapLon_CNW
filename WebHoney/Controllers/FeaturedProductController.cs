using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
[Route("Admin/FeaturedProduct")]
public class FeaturedProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FeaturedProductController> _logger;

    public FeaturedProductController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<FeaturedProductController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // GET: FeaturedProduct
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var featuredProducts = await _context.FeaturedProducts
            .Include(fp => fp.Product)
            .OrderBy(fp => fp.DisplayOrder)
            .ThenByDescending(fp => fp.CreatedAt)
            .ToListAsync();
        return View(featuredProducts);
    }

    // GET: FeaturedProduct/Create
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name");
        return View();
    }

    // POST: FeaturedProduct/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? imageFile, string? imageUrl, long? productId, [Bind("Title,Subtitle,Description,DisplayOrder,IsActive")] FeaturedProduct featuredProduct)
    {
        // Xử lý ProductId
        if (productId.HasValue && productId.Value > 0)
        {
            featuredProduct.ProductId = productId.Value;
        }

        // Xử lý upload ảnh hoặc URL
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            featuredProduct.ImageUrl = imageUrl.Trim();
        }
        else if (imageFile != null && imageFile.Length > 0)
        {
            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
                return View(featuredProduct);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
                return View(featuredProduct);
            }

            try
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "featured");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                featuredProduct.ImageUrl = $"/uploads/featured/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh featured product");
                ModelState.AddModelError("ImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
                return View(featuredProduct);
            }
        }

        // Kiểm tra nếu không có ảnh và không có URL
        if (string.IsNullOrWhiteSpace(featuredProduct.ImageUrl))
        {
            ModelState.AddModelError("ImageUrl", "Vui lòng chọn ảnh hoặc nhập URL hình ảnh.");
        }

        // Xóa các lỗi validation không cần thiết
        ModelState.Remove("Product");
        ModelState.Remove("ProductId");

        if (ModelState.IsValid)
        {
            try
        {
            featuredProduct.CreatedAt = DateTime.Now;
            _context.Add(featuredProduct);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sản phẩm nổi bật thành công!";
            return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi database khi lưu featured product");
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu vào database. Vui lòng kiểm tra lại dữ liệu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu featured product");
                ModelState.AddModelError("", $"Có lỗi xảy ra khi lưu sản phẩm nổi bật: {ex.Message}");
            }
        }
        else
        {
            // Log các lỗi validation để debug
            foreach (var error in ModelState)
            {
                foreach (var err in error.Value.Errors)
                {
                    _logger.LogWarning("Validation error for {Key}: {Error}", error.Key, err.ErrorMessage);
                }
            }
        }

        ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
        return View(featuredProduct);
    }

    // GET: FeaturedProduct/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null) return NotFound();

        var featuredProduct = await _context.FeaturedProducts.FindAsync(id);
        if (featuredProduct == null) return NotFound();

        ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", featuredProduct.ProductId);
        return View(featuredProduct);
    }

    // POST: FeaturedProduct/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, IFormFile? imageFile, string? imageUrl, long? productId, [Bind("Id,ImageUrl,Title,Subtitle,Description,DisplayOrder,IsActive,CreatedAt")] FeaturedProduct featuredProduct)
    {
        if (id != featuredProduct.Id) return NotFound();

        try
        {
            var existing = await _context.FeaturedProducts.FindAsync(id);
            if (existing == null) return NotFound();

            // Xử lý ProductId
            if (productId.HasValue && productId.Value > 0)
            {
                existing.ProductId = productId.Value;
            }
            else
            {
                existing.ProductId = null;
            }

            // Xử lý ảnh
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                // Xóa file cũ nếu là file local
                if (!string.IsNullOrEmpty(existing.ImageUrl) && existing.ImageUrl.StartsWith("/uploads/"))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch { }
                    }
                }
                existing.ImageUrl = imageUrl.Trim();
            }
            else if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                    ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
                    return View(featuredProduct);
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                    ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
                    return View(featuredProduct);
                }

                // Xóa file cũ
                if (!string.IsNullOrEmpty(existing.ImageUrl) && existing.ImageUrl.StartsWith("/uploads/"))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch { }
                    }
                }

                // Lưu file mới
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "featured");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existing.ImageUrl = $"/uploads/featured/{fileName}";
            }

            // Cập nhật các thông tin khác
            existing.Title = featuredProduct.Title;
            existing.Subtitle = featuredProduct.Subtitle;
            existing.Description = featuredProduct.Description;
            existing.DisplayOrder = featuredProduct.DisplayOrder;
            existing.IsActive = featuredProduct.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật sản phẩm nổi bật thành công!";
            TempData["ShowSuccessModal"] = true;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật featured product");
            ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.");
            ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", productId);
            return View(featuredProduct);
        }
    }

    // POST: FeaturedProduct/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var featuredProduct = await _context.FeaturedProducts.FindAsync(id);
        if (featuredProduct != null)
        {
            // Xóa file ảnh
            if (!string.IsNullOrEmpty(featuredProduct.ImageUrl) && featuredProduct.ImageUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_env.WebRootPath, featuredProduct.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch { }
                }
            }

            _context.FeaturedProducts.Remove(featuredProduct);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa sản phẩm nổi bật thành công!";
            TempData["ShowSuccessModal"] = true;
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: FeaturedProduct/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var featuredProduct = await _context.FeaturedProducts.FindAsync(id);
        if (featuredProduct == null) return NotFound();

        featuredProduct.IsActive = !featuredProduct.IsActive;
        featuredProduct.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = featuredProduct.IsActive ? "Kích hoạt thành công!" : "Ẩn thành công!";
        TempData["ShowSuccessModal"] = true;
        return RedirectToAction(nameof(Index));
    }
}

