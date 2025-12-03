using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class ContactMessageController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContactMessageController> _logger;

    public ContactMessageController(ApplicationDbContext context, ILogger<ContactMessageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: ContactMessage
    public async Task<IActionResult> Index(int page = 1, string status = "ALL")
    {
        const int pageSize = 10;
        var query = _context.ContactMessages
            .Include(cm => cm.User)
            .AsQueryable();

        // Lọc theo status
        if (!string.IsNullOrEmpty(status) && status != "ALL")
        {
            query = query.Where(cm => cm.Status == status);
        }

        // Sắp xếp theo thời gian mới nhất
        query = query.OrderByDescending(cm => cm.CreatedAt);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["CurrentStatus"] = status;

        return View(messages);
    }

    // GET: ContactMessage/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var contactMessage = await _context.ContactMessages
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (contactMessage == null)
        {
            return NotFound();
        }

        // Đánh dấu là đã đọc nếu chưa đọc
        if (contactMessage.Status == "NEW")
        {
            contactMessage.Status = "IN_PROGRESS";
            contactMessage.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return View(contactMessage);
    }

    // POST: ContactMessage/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        var contactMessage = await _context.ContactMessages.FindAsync(id);
        if (contactMessage == null)
        {
            return NotFound();
        }

        if (status == "NEW" || status == "IN_PROGRESS" || status == "RESOLVED")
        {
            contactMessage.Status = status;
            contactMessage.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái thành công.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: ContactMessage/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var contactMessage = await _context.ContactMessages.FindAsync(id);
        if (contactMessage != null)
        {
            _context.ContactMessages.Remove(contactMessage);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa tin nhắn thành công.";
        }

        return RedirectToAction(nameof(Index));
    }
}

