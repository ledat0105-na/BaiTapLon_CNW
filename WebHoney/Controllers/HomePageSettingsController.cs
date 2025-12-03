using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize]
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
        var settings = await _context.HomePageSettings.FindAsync(1);
        if (settings == null)
        {
            // Tạo mới nếu chưa có
            settings = new HomePageSettings
            {
                Id = 1,
                FeaturedImageUrl = "/assets/images/featured.jpg"
            };
            _context.HomePageSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return View(settings);
    }

    // POST: HomePageSettings/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, IFormFile? imageFile, string? imageUrl)
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

        // Xử lý upload file
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "homepage");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
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
                    System.IO.File.Delete(oldFilePath);
                }
            }

            settings.FeaturedImageUrl = $"/uploads/homepage/{uniqueFileName}";
        }
        else if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            settings.FeaturedImageUrl = imageUrl.Trim();
        }

        settings.UpdatedAt = DateTime.Now;

        try
        {
            _context.Update(settings);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật ảnh giới thiệu thành công.";
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

