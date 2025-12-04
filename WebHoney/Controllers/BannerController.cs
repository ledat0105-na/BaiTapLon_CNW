using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class BannerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BannerController> _logger;

    public BannerController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<BannerController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    // GET: Banner
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 5;
        var banners = _context.BannerImages
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .AsQueryable();

        var totalItems = await banners.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedBanners = await banners
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedBanners);
    }

    // GET: Banner/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Banner/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IFormFile? imageFile, string? imageUrl, [Bind("Title,Subtitle,DisplayOrder,IsActive")] BannerImage banner)
    {
        try
        {
            // Trường hợp dùng URL ảnh bên ngoài hoặc ảnh có sẵn trong wwwroot
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                banner.ImageUrl = imageUrl.Trim();
                banner.CreatedAt = DateTime.Now;

                if (banner.DisplayOrder == 0)
                {
                    var maxOrder = await _context.BannerImages
                        .Select(b => (int?)b.DisplayOrder)
                        .DefaultIfEmpty(0)
                        .MaxAsync();
                    banner.DisplayOrder = maxOrder.Value + 1;
                }

                _context.Add(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm banner thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu không có URL thì bắt buộc phải upload file
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh banner hoặc nhập URL ảnh.");
                return View(banner);
            }

            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                return View(banner);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                return View(banner);
            }

            // Tạo thư mục uploads/banners nếu chưa có
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "banners");
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
            banner.ImageUrl = $"/uploads/banners/{fileName}";
            banner.CreatedAt = DateTime.Now;

            // Nếu không có DisplayOrder, lấy giá trị lớn nhất + 1
            if (banner.DisplayOrder == 0)
            {
                var maxOrder = await _context.BannerImages
                    .Select(b => (int?)b.DisplayOrder)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                banner.DisplayOrder = maxOrder.Value + 1;
            }

            _context.Add(banner);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Thêm banner thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo banner");
            ModelState.AddModelError("", "Có lỗi xảy ra khi tạo banner. Vui lòng thử lại.");
            return View(banner);
        }
    }

    // GET: Banner/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null) return NotFound();
        var banner = await _context.BannerImages.FindAsync(id);
        if (banner == null) return NotFound();
        return View(banner);
    }

    // POST: Banner/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, IFormFile? imageFile, [Bind("Id,ImageUrl,Title,Subtitle,DisplayOrder,IsActive,CreatedAt")] BannerImage banner)
    {
        if (id != banner.Id) return NotFound();

        try
        {
            var existingBanner = await _context.BannerImages.FindAsync(id);
            if (existingBanner == null) return NotFound();

            // Ưu tiên URL nếu có, sau đó mới đến file upload
            var newImageUrl = banner.ImageUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(newImageUrl))
            {
                // Chỉ cập nhật nếu URL thay đổi
                if (existingBanner.ImageUrl != newImageUrl)
                {
                    // Xóa file cũ nếu là file local
                    if (!string.IsNullOrEmpty(existingBanner.ImageUrl) && existingBanner.ImageUrl.StartsWith("/uploads/"))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, existingBanner.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Không thể xóa file cũ: {FilePath}", oldFilePath);
                            }
                        }
                    }
                    existingBanner.ImageUrl = newImageUrl;
                    existingBanner.UpdatedAt = DateTime.Now; // Cập nhật timestamp khi đổi ảnh
                }
            }
            // Nếu có file mới được upload (và không có URL)
            else if (imageFile != null && imageFile.Length > 0)
            {
                // Kiểm tra định dạng
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                    return View(banner);
                }

                // Kiểm tra kích thước
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                    return View(banner);
                }

                // Xóa file cũ nếu là file local
                if (!string.IsNullOrEmpty(existingBanner.ImageUrl) && existingBanner.ImageUrl.StartsWith("/uploads/"))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, existingBanner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Không thể xóa file cũ: {FilePath}", oldFilePath);
                        }
                    }
                }

                // Lưu file mới
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "banners");
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

                existingBanner.ImageUrl = $"/uploads/banners/{fileName}";
                existingBanner.UpdatedAt = DateTime.Now; // Cập nhật timestamp khi upload file mới
            }
            // Nếu không có file upload và không có URL, giữ nguyên ImageUrl hiện tại

            // Cập nhật các thông tin khác
            var hasChanges = existingBanner.Title != banner.Title ||
                            existingBanner.Subtitle != banner.Subtitle ||
                            existingBanner.DisplayOrder != banner.DisplayOrder ||
                            existingBanner.IsActive != banner.IsActive;

            existingBanner.Title = banner.Title;
            existingBanner.Subtitle = banner.Subtitle;
            existingBanner.DisplayOrder = banner.DisplayOrder;
            existingBanner.IsActive = banner.IsActive;
            
            // Chỉ cập nhật UpdatedAt nếu có thay đổi hoặc chưa được cập nhật ở trên
            if (hasChanges && existingBanner.UpdatedAt == null)
            {
                existingBanner.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật banner thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật banner");
            ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.");
            return View(banner);
        }
    }

    // POST: Banner/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var banner = await _context.BannerImages.FindAsync(id);
        if (banner != null)
        {
            // Xóa file ảnh
            if (!string.IsNullOrEmpty(banner.ImageUrl))
            {
                var filePath = Path.Combine(_env.WebRootPath, banner.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.BannerImages.Remove(banner);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa banner thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Banner/Reorder
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Reorder([FromBody] List<BannerOrderDto> orders)
    {
        try
        {
            foreach (var order in orders)
            {
                var banner = await _context.BannerImages.FindAsync(order.Id);
                if (banner != null)
                {
                    banner.DisplayOrder = order.Order;
                    banner.UpdatedAt = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Sắp xếp banner thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sắp xếp banner");
            return Json(new { success = false, message = "Có lỗi xảy ra khi sắp xếp." });
        }
    }

    // POST: Banner/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var banner = await _context.BannerImages.FindAsync(id);
        if (banner == null) return NotFound();

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = banner.IsActive ? "Kích hoạt banner thành công!" : "Vô hiệu hóa banner thành công!";
        return RedirectToAction(nameof(Index));
    }
}

// DTO cho reorder
public class BannerOrderDto
{
    public long Id { get; set; }
    public int Order { get; set; }
}

