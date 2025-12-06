using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.Extensions;

namespace WebHoney.Controllers;

public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(ApplicationDbContext context, ILogger<NotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Notification/GetUnreadCount - Lấy số lượng thông báo chưa đọc
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { count = 0 });
        }

        var count = await _context.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .CountAsync();

        return Json(new { count });
    }

    // GET: Notification/GetNotifications - Lấy danh sách thông báo
    [HttpGet]
    public async Task<IActionResult> GetNotifications(int limit = 10)
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { notifications = new List<object>() });
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                isRead = n.IsRead,
                createdAt = n.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                relatedId = n.RelatedId,
                relatedType = n.RelatedType
            })
            .ToListAsync();

        return Json(new { notifications });
    }

    // POST: Notification/MarkAsRead - Đánh dấu thông báo là đã đọc
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập." });
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

        if (notification == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thông báo." });
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // POST: Notification/MarkAllAsRead - Đánh dấu tất cả thông báo là đã đọc
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập." });
        }

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, count = unreadNotifications.Count });
    }
}

