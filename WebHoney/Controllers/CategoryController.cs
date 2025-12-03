using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ApplicationDbContext context, ILogger<CategoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Category
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 5;
        var categories = _context.Categories
            .Include(c => c.Products)
            .OrderByDescending(c => c.CreatedAt)
            .AsQueryable();

        var totalItems = await categories.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedCategories = await categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedCategories);
    }

    // GET: Category/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null) return NotFound();

        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (category == null) return NotFound();

        return View(category);
    }

    // GET: Category/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
    {
        if (ModelState.IsValid)
        {
            category.Slug = GenerateSlug(category.Name);
            category.IsActive = true;
            category.CreatedAt = DateTime.Now;
            _context.Add(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Category/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null) return NotFound();
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    // POST: Category/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Id,Name,Description,IsActive,CreatedAt")] Category category)
    {
        if (id != category.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                category.Slug = GenerateSlug(category.Name);
                category.UpdatedAt = DateTime.Now;
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // POST: Category/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = category.IsActive ? "Kích hoạt danh mục thành công!" : "Vô hiệu hóa danh mục thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: Category/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null) return NotFound();
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (category == null) return NotFound();
        return View(category);
    }

    // POST: Category/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CategoryExists(long id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("Đ", "d");
    }
}

