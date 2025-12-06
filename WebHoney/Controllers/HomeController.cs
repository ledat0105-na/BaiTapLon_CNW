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
        // Load tất cả banner để debug
        var allBanners = await _context.BannerImages.ToListAsync();
        _logger.LogInformation($"Total banners in database: {allBanners.Count}");
        
        foreach (var banner in allBanners)
        {
            _logger.LogInformation($"Banner ID: {banner.Id}, IsActive: {banner.IsActive}, ImageUrl: {banner.ImageUrl}, DisplayOrder: {banner.DisplayOrder}");
        }
        
        // Load banner images từ database - lấy banner có ImageUrl (ưu tiên IsActive = true)
        var banners = await _context.BannerImages
            .Where(b => !string.IsNullOrEmpty(b.ImageUrl))
            .OrderBy(b => b.IsActive ? 0 : 1) // Ưu tiên banner đang active
            .ThenBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.UpdatedAt ?? b.CreatedAt)
            .ToListAsync();
        
        // Log để debug
        _logger.LogInformation($"Loaded {banners.Count} active banners from database");
        foreach (var banner in banners)
        {
            _logger.LogInformation($"Active Banner ID: {banner.Id}, ImageUrl: {banner.ImageUrl}, Title: {banner.Title}");
        }

        // Load featured products từ database (từ phần "We Provide The Best Property You Like" trở xuống)
        var featuredProducts = await _context.FeaturedProducts
            .Include(fp => fp.Product!)
                .ThenInclude(p => p.Category)
            .Where(fp => fp.IsActive)
            .OrderBy(fp => fp.DisplayOrder)
            .ThenByDescending(fp => fp.CreatedAt)
            .ToListAsync();

        // Load homepage settings (ảnh giới thiệu và sản phẩm bán chạy)
        var homePageSettings = await _context.HomePageSettings
            .Include(h => h.BestSellerProduct)
            .FirstOrDefaultAsync(h => h.Id == 1);
            
        if (homePageSettings == null)
        {
            // Tạo mới nếu chưa có
            homePageSettings = new HomePageSettings
            {
                Id = 1,
                FeaturedImageUrl = "/assets/images/featured.jpg",
                BestSellerImageUrl = "/assets/images/deal-01.jpg",
                BestSellerTitle = "Mật Ong Hoa Nhãn",
                BestSellerDescription = "Mật ong hoa nhãn với hương vị đặc trưng, ngọt thanh tự nhiên. Sản phẩm được thu hoạch từ các vườn nhãn tại Tây Nguyên, đảm bảo chất lượng và độ tinh khiết cao nhất."
            };
            _context.HomePageSettings.Add(homePageSettings);
            await _context.SaveChangesAsync();
        }

        ViewData["Banners"] = banners;
        ViewData["FeaturedProducts"] = featuredProducts;
        ViewData["FeaturedImageUrl"] = homePageSettings.FeaturedImageUrl ?? "/assets/images/featured.jpg";
        ViewData["HomePageSettings"] = homePageSettings;
        ViewData["BannerSlideInterval"] = homePageSettings.BannerSlideInterval; // Thời gian chuyển slide (mili giây)
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
