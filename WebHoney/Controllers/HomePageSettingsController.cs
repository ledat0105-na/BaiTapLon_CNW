using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class HomePageSettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<HomePageSettingsController> _logger;

    public HomePageSettingsController(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment,
        ILogger<HomePageSettingsController> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    // GET: HomePageSettings/Edit
    public async Task<IActionResult> Edit()
    {
        var settings = await _context.HomePageSettings
            .Include(h => h.BestSellerProduct)
            .Include(h => h.NewArrivalProduct)
            .Include(h => h.SpecialOfferProduct)
            .FirstOrDefaultAsync(h => h.Id == 1);
            
        if (settings == null)
        {
            // Tạo mới nếu chưa có
            settings = new HomePageSettings
            {
                Id = 1,
                FeaturedImageUrl = "/assets/images/featured.jpg",
                BestSellerImageUrl = "/assets/images/deal-01.jpg",
                BestSellerTitle = "Mật Ong Hoa Nhãn",
                BestSellerDescription = "Mật ong hoa nhãn với hương vị đặc trưng, ngọt thanh tự nhiên. Sản phẩm được thu hoạch từ các vườn nhãn tại Tây Nguyên, đảm bảo chất lượng và độ tinh khiết cao nhất.",
                NewArrivalImageUrl = "/assets/images/deal-02.jpg",
                NewArrivalTitle = "Mật Ong Rừng",
                NewArrivalDescription = "Mật ong rừng nguyên chất từ các khu rừng nguyên sinh, mang hương vị đậm đà và nhiều dưỡng chất quý giá. Sản phẩm được thu hoạch thủ công, đảm bảo chất lượng cao nhất.",
                SpecialOfferImageUrl = "/assets/images/deal-03.jpg",
                SpecialOfferTitle = "Combo Đặc Biệt",
                SpecialOfferDescription = "Combo đặc biệt với nhiều ưu đãi hấp dẫn. Mua ngay để nhận được giá tốt nhất và quà tặng kèm theo.",
                BannerSlideInterval = 5000 // Mặc định 5 giây
            };
            _context.HomePageSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        // Load danh sách sản phẩm cho dropdown
        ViewBag.Products = new SelectList(
            await _context.Products.Where(p => p.IsActive).ToListAsync(), 
            "Id", 
            "Name", 
            settings.BestSellerProductId);

        return View(settings);
    }

    // POST: HomePageSettings/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, 
        IFormFile? imageFile, 
        string? imageUrl,
        IFormFile? bestSellerImageFile,
        string? bestSellerImageUrl,
        long? bestSellerProductId,
        string? bestSellerTitle,
        string? bestSellerDescription,
        IFormFile? newArrivalImageFile,
        string? newArrivalImageUrl,
        long? newArrivalProductId,
        string? newArrivalTitle,
        string? newArrivalDescription,
        IFormFile? specialOfferImageFile,
        string? specialOfferImageUrl,
        long? specialOfferProductId,
        string? specialOfferTitle,
        string? specialOfferDescription,
        int? bannerSlideInterval)
    {
        if (id != 1)
        {
            return NotFound();
        }

        var settings = await _context.HomePageSettings.FindAsync(id);
        if (settings == null)
        {
            settings = new HomePageSettings { Id = 1 };
            _context.HomePageSettings.Add(settings);
        }

        // Xử lý upload ảnh giới thiệu (featured image)
        if (imageFile != null && imageFile.Length > 0)
        {
            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                return View(settings);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB.");
                return View(settings);
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "homepage");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(settings.FeaturedImageUrl) && 
                    settings.FeaturedImageUrl.StartsWith("/uploads/homepage/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, settings.FeaturedImageUrl.TrimStart('/'));
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

                settings.FeaturedImageUrl = $"/uploads/homepage/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh homepage");
                ModelState.AddModelError("ImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                return View(settings);
            }
        }
        else if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            settings.FeaturedImageUrl = imageUrl.Trim();
        }

        // Xử lý upload ảnh sản phẩm bán chạy
        if (bestSellerImageFile != null && bestSellerImageFile.Length > 0)
        {
            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(bestSellerImageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("BestSellerImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.Products = new SelectList(
                    await _context.Products.Where(p => p.IsActive).ToListAsync(), 
                    "Id", 
                    "Name", 
                    bestSellerProductId);
                return View(settings);
            }

            // Kiểm tra kích thước file (max 5MB)
            if (bestSellerImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("BestSellerImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.Products = new SelectList(
                    await _context.Products.Where(p => p.IsActive).ToListAsync(), 
                    "Id", 
                    "Name", 
                    bestSellerProductId);
                return View(settings);
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "homepage");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await bestSellerImageFile.CopyToAsync(fileStream);
                }

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(settings.BestSellerImageUrl) && 
                    settings.BestSellerImageUrl.StartsWith("/uploads/homepage/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, settings.BestSellerImageUrl.TrimStart('/'));
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

                settings.BestSellerImageUrl = $"/uploads/homepage/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh best seller");
                ModelState.AddModelError("BestSellerImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.Products = new SelectList(
                    await _context.Products.Where(p => p.IsActive).ToListAsync(), 
                    "Id", 
                    "Name", 
                    bestSellerProductId);
                return View(settings);
            }
        }
        else if (!string.IsNullOrWhiteSpace(bestSellerImageUrl))
        {
            settings.BestSellerImageUrl = bestSellerImageUrl.Trim();
        }

        // Cập nhật thông tin sản phẩm bán chạy
        if (bestSellerProductId.HasValue && bestSellerProductId.Value > 0)
        {
            settings.BestSellerProductId = bestSellerProductId.Value;
        }
        else
        {
            settings.BestSellerProductId = null;
        }

        if (!string.IsNullOrWhiteSpace(bestSellerTitle))
        {
            settings.BestSellerTitle = bestSellerTitle.Trim();
        }

        if (!string.IsNullOrWhiteSpace(bestSellerDescription))
        {
            settings.BestSellerDescription = bestSellerDescription.Trim();
        }

        // Xử lý upload ảnh sản phẩm mới
        if (newArrivalImageFile != null && newArrivalImageFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(newArrivalImageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("NewArrivalImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", newArrivalProductId);
                return View(settings);
            }

            if (newArrivalImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("NewArrivalImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", newArrivalProductId);
                return View(settings);
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "homepage");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await newArrivalImageFile.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(settings.NewArrivalImageUrl) && 
                    settings.NewArrivalImageUrl.StartsWith("/uploads/homepage/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, settings.NewArrivalImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Không thể xóa file cũ: {FilePath}", oldFilePath); }
                    }
                }

                settings.NewArrivalImageUrl = $"/uploads/homepage/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh new arrival");
                ModelState.AddModelError("NewArrivalImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", newArrivalProductId);
                return View(settings);
            }
        }
        else if (!string.IsNullOrWhiteSpace(newArrivalImageUrl))
        {
            settings.NewArrivalImageUrl = newArrivalImageUrl.Trim();
        }

        // Cập nhật thông tin sản phẩm mới
        if (newArrivalProductId.HasValue && newArrivalProductId.Value > 0)
        {
            settings.NewArrivalProductId = newArrivalProductId.Value;
        }
        else
        {
            settings.NewArrivalProductId = null;
        }

        if (!string.IsNullOrWhiteSpace(newArrivalTitle))
        {
            settings.NewArrivalTitle = newArrivalTitle.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newArrivalDescription))
        {
            settings.NewArrivalDescription = newArrivalDescription.Trim();
        }

        // Xử lý upload ảnh khuyến mãi
        if (specialOfferImageFile != null && specialOfferImageFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(specialOfferImageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("SpecialOfferImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, webp).");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", specialOfferProductId);
                return View(settings);
            }

            if (specialOfferImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("SpecialOfferImageFile", "Kích thước file không được vượt quá 5MB.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", specialOfferProductId);
                return View(settings);
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "homepage");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await specialOfferImageFile.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(settings.SpecialOfferImageUrl) && 
                    settings.SpecialOfferImageUrl.StartsWith("/uploads/homepage/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, settings.SpecialOfferImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Không thể xóa file cũ: {FilePath}", oldFilePath); }
                    }
                }

                settings.SpecialOfferImageUrl = $"/uploads/homepage/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload ảnh special offer");
                ModelState.AddModelError("SpecialOfferImageFile", "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.");
                ViewBag.Products = new SelectList(await _context.Products.Where(p => p.IsActive).ToListAsync(), "Id", "Name", specialOfferProductId);
                return View(settings);
            }
        }
        else if (!string.IsNullOrWhiteSpace(specialOfferImageUrl))
        {
            settings.SpecialOfferImageUrl = specialOfferImageUrl.Trim();
        }

        // Cập nhật thông tin khuyến mãi
        if (specialOfferProductId.HasValue && specialOfferProductId.Value > 0)
        {
            settings.SpecialOfferProductId = specialOfferProductId.Value;
        }
        else
        {
            settings.SpecialOfferProductId = null;
        }

        if (!string.IsNullOrWhiteSpace(specialOfferTitle))
        {
            settings.SpecialOfferTitle = specialOfferTitle.Trim();
        }

        if (!string.IsNullOrWhiteSpace(specialOfferDescription))
        {
            settings.SpecialOfferDescription = specialOfferDescription.Trim();
        }

        // Cập nhật thời gian chuyển slide banner
        if (bannerSlideInterval.HasValue)
        {
            // Chuyển đổi từ giây sang mili giây, hoặc giữ nguyên nếu đã là mili giây
            // Nếu giá trị < 1000, coi như là giây và nhân 1000
            // Nếu giá trị >= 1000, coi như đã là mili giây
            if (bannerSlideInterval.Value == 0)
            {
                settings.BannerSlideInterval = 0; // Không tự động chuyển
            }
            else if (bannerSlideInterval.Value < 1000)
            {
                settings.BannerSlideInterval = bannerSlideInterval.Value * 1000; // Chuyển từ giây sang mili giây
            }
            else
            {
                settings.BannerSlideInterval = bannerSlideInterval.Value; // Giữ nguyên nếu đã là mili giây
            }
        }

        settings.UpdatedAt = DateTime.Now;

        try
        {
            _context.Update(settings);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật cài đặt trang chủ thành công.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!HomePageSettingsExists(settings.Id))
            {
                return NotFound();
            }
            throw;
        }

        return RedirectToAction(nameof(Edit));
    }

    private bool HomePageSettingsExists(int id)
    {
        return _context.HomePageSettings.Any(e => e.Id == id);
    }
}

