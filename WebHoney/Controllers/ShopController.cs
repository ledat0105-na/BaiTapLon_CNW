using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

public class ShopController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShopController> _logger;

    public ShopController(ApplicationDbContext context, ILogger<ShopController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Shop - Danh sách sản phẩm
    public async Task<IActionResult> Index(string? search, long? categoryId, string? sortBy, int page = 1, int pageSize = 9)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock > 0)
            .AsQueryable();

        // Tìm kiếm theo tên
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || 
                                    (p.ShortDesc != null && p.ShortDesc.Contains(search)) ||
                                    (p.Description != null && p.Description.Contains(search)));
        }

        // Lọc theo danh mục
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // Sắp xếp
        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // Mặc định: mới nhất
        };

        // Lấy tổng số sản phẩm
        var totalItems = await query.CountAsync();

        // Phân trang
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy danh sách danh mục để hiển thị filter
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewData["Categories"] = categories;
        ViewData["Search"] = search;
        ViewData["CategoryId"] = categoryId;
        ViewData["SortBy"] = sortBy;
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalItems"] = totalItems;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalItems / (double)pageSize);

        return View(products);
    }

    // GET: Shop/Details/5 - Chi tiết sản phẩm
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null)
        {
            return NotFound();
        }

        // Lấy sản phẩm liên quan (cùng danh mục)
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == product.CategoryId && 
                       p.Id != product.Id && 
                       p.IsActive && 
                       p.Stock > 0)
            .Take(4)
            .ToListAsync();

        ViewData["RelatedProducts"] = relatedProducts;

        return View(product);
    }
}

