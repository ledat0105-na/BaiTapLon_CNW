using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderController> _logger;

    public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Order
    public async Task<IActionResult> Index(string? statusFilter, string? searchString, int page = 1)
    {
        const int pageSize = 5;
        var orders = _context.Orders
            .Include(o => o.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            orders = orders.Where(o => o.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            orders = orders.Where(o => 
                o.ShippingAddress!.Contains(searchString) ||
                o.Phone!.Contains(searchString) ||
                (o.Customer != null && o.Customer.FullName.Contains(searchString))
            );
        }

        orders = orders.OrderByDescending(o => o.CreatedAt);

        var totalItems = await orders.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedOrders = await orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["StatusFilter"] = statusFilter;
        ViewData["SearchString"] = searchString;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedOrders);
    }

    // GET: Order/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    // POST: Order/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        order.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool OrderExists(long id)
    {
        return _context.Orders.Any(e => e.Id == id);
    }
}

