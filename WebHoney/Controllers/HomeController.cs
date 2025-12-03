using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index()
    {
        // Load banner images từ database
        var banners = await _context.BannerImages
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
            .ToListAsync();

        // Load featured products từ database (từ phần "We Provide The Best Property You Like" trở xuống)
        var featuredProducts = await _context.FeaturedProducts
            .Include(fp => fp.Product!)
                .ThenInclude(p => p.Category)
            .Where(fp => fp.IsActive)
            .OrderBy(fp => fp.DisplayOrder)
            .ThenByDescending(fp => fp.CreatedAt)
            .ToListAsync();

        // Load homepage settings (ảnh giới thiệu)
        var homePageSettings = await _context.HomePageSettings.FindAsync(1);
        if (homePageSettings == null)
        {
            // Tạo mới nếu chưa có
            homePageSettings = new HomePageSettings
            {
                Id = 1,
                FeaturedImageUrl = "/assets/images/featured.jpg"
            };
            _context.HomePageSettings.Add(homePageSettings);
            await _context.SaveChangesAsync();
        }

        ViewData["Banners"] = banners;
        ViewData["FeaturedProducts"] = featuredProducts;
        ViewData["FeaturedImageUrl"] = homePageSettings.FeaturedImageUrl ?? "/assets/images/featured.jpg";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Properties()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
