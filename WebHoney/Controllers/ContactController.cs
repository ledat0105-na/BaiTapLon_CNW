using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

public class ContactController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContactController> _logger;

    public ContactController(ApplicationDbContext context, ILogger<ContactController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string name, string email, string? subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(message))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc (Tên và Nội dung).";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Lấy UserId nếu đã đăng nhập
            long? userId = null;
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    userId = user.Id;
                }
            }

            var contactMessage = new ContactMessage
            {
                UserId = userId,
                Name = name.Trim(),
                Email = !string.IsNullOrWhiteSpace(email) ? email.Trim() : null,
                Subject = !string.IsNullOrWhiteSpace(subject) ? subject.Trim() : null,
                Message = message.Trim(),
                Status = "NEW",
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
            _logger.LogInformation("New contact message received from {Name} ({Email})", name, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving contact message");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại sau.";
        }

        return RedirectToAction("Index", "Home");
    }
}

