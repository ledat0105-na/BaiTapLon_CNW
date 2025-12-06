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
    public async Task<IActionResult> Details(long? id, string? returnUrl = null)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();

        ViewData["ReturnUrl"] = returnUrl;
        return View(order);
    }

    // POST: Order/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, string status, string? rejectionReason = null)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.Now;
        
        // Lưu lý do từ chối nếu có
        if (status == "CANCELED" && !string.IsNullOrWhiteSpace(rejectionReason))
        {
            order.RejectionReason = rejectionReason.Trim();
        }
        else if (status != "CANCELED")
        {
            order.RejectionReason = null;
        }

        await _context.SaveChangesAsync();

        // Tạo thông báo cho khách hàng về thay đổi trạng thái đơn hàng
        if (order.UserId.HasValue)
        {
            string notificationTitle = "";
            string notificationMessage = "";
            string notificationType = "INFO";

            switch (status)
            {
                case "PROCESSING":
                    notificationTitle = "Đơn hàng đã được xác nhận";
                    notificationMessage = $"Đơn hàng #{order.Id} của bạn đã được xác nhận và đang được xử lý.";
                    notificationType = "SUCCESS";
                    break;
                case "SHIPPING":
                    notificationTitle = "Đơn hàng đang được giao";
                    notificationMessage = $"Đơn hàng #{order.Id} của bạn đang được vận chuyển đến địa chỉ giao hàng.";
                    notificationType = "SUCCESS";
                    break;
                case "COMPLETED":
                    notificationTitle = "Đơn hàng đã hoàn thành";
                    notificationMessage = $"Đơn hàng #{order.Id} của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm!";
                    notificationType = "SUCCESS";
                    break;
                case "CANCELED":
                    notificationTitle = "Đơn hàng đã bị hủy";
                    notificationMessage = $"Đơn hàng #{order.Id} của bạn đã bị hủy.";
                    if (!string.IsNullOrWhiteSpace(rejectionReason))
                    {
                        notificationMessage += $" Lý do: {rejectionReason}";
                    }
                    notificationType = "ERROR";
                    break;
                default:
                    notificationTitle = "Cập nhật trạng thái đơn hàng";
                    notificationMessage = $"Trạng thái đơn hàng #{order.Id} đã được cập nhật thành: {status}";
                    break;
            }

            var notification = new Notification
            {
                UserId = order.UserId.Value,
                Title = notificationTitle,
                Message = notificationMessage,
                Type = notificationType,
                RelatedId = order.Id,
                RelatedType = "ORDER",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool OrderExists(long id)
    {
        return _context.Orders.Any(e => e.Id == id);
    }
}

