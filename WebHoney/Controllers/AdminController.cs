using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models.ViewModels;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // Đơn hàng hoàn thành hôm nay
        var completedToday = _context.Orders
            .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow && o.Status == "COMPLETED");

        var ordersToday = completedToday.Count();

        // Doanh thu hôm nay (chỉ tính đơn COMPLETED)
        var revenueToday = completedToday
            .Sum(o => (decimal?)o.TotalAmount) ?? 0;

        // Sản phẩm sắp hết (stock <= 10)
        var lowStockProducts = _context.Products
            .Include(p => p.Category)
            .Where(p => p.Stock <= 10 && p.IsActive)
            .OrderBy(p => p.Stock)
            .ToList();

        // Tổng số đơn hàng đang xử lý (PENDING, PROCESSING)
        var pendingOrders = _context.Orders
            .Where(o => o.Status == "PENDING" || o.Status == "PROCESSING")
            .Count();

        // Tổng doanh thu tháng này (chỉ tính COMPLETED)
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var completedThisMonth = _context.Orders
            .Where(o => o.CreatedAt >= firstDayOfMonth && o.CreatedAt < tomorrow && o.Status == "COMPLETED");

        var revenueThisMonth = completedThisMonth
            .Sum(o => (decimal?)o.TotalAmount) ?? 0;

        // Một số thống kê tổng quan
        var totalOrders = _context.Orders.Count();
        var totalCustomers = _context.Customers.Count();
        var totalProducts = _context.Products.Count(p => p.IsActive);

        // Đơn đang giao (SHIPPING)
        var shippingOrders = _context.Orders
            .Where(o => o.Status == "SHIPPING")
            .Count();

        // Đơn bị hủy (CANCELED)
        var canceledOrders = _context.Orders
            .Where(o => o.Status == "CANCELED")
            .Count();

        // Top sản phẩm bán chạy (theo số lượng trong các đơn COMPLETED)
        var topProducts = _context.OrderDetails
            .Include(od => od.Product)
            .Include(od => od.Order)
            .Where(od => od.Order.Status == "COMPLETED")
            .GroupBy(od => new { od.ProductId, od.ProductName })
            .Select(g => new TopProductViewModel
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(tp => tp.TotalQuantity)
            .Take(10)
            .ToList();

        ViewData["OrdersToday"] = ordersToday;
        ViewData["RevenueToday"] = revenueToday;
        ViewData["LowStockProducts"] = lowStockProducts;
        ViewData["PendingOrders"] = pendingOrders;
        ViewData["RevenueThisMonth"] = revenueThisMonth;
        ViewData["TotalOrders"] = totalOrders;
        ViewData["TotalCustomers"] = totalCustomers;
        ViewData["TotalProducts"] = totalProducts;
        ViewData["ShippingOrders"] = shippingOrders;
        ViewData["CanceledOrders"] = canceledOrders;
        ViewData["TopProducts"] = topProducts;

        return View();
    }

    public IActionResult Users()
    {
        // Chỉ Admin mới được xem
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var users = _context.Users.ToList();
        return View(users);
    }

    public IActionResult Reports(string? period = "month", string? sortBy = "revenue")
    {
        var orders = _context.Orders.AsQueryable();
        var now = DateTime.Now;
        DateTime startDate;
        DateTime endDate = now;

        // Xác định khoảng thời gian
        switch (period?.ToLower())
        {
            case "day":
                startDate = now.Date;
                endDate = now.Date.AddDays(1);
                ViewData["PeriodLabel"] = "Hôm nay";
                break;
            case "week":
                startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                ViewData["PeriodLabel"] = "Tuần này";
                break;
            case "year":
                startDate = new DateTime(now.Year, 1, 1);
                ViewData["PeriodLabel"] = "Năm nay";
                break;
            default: // month
                startDate = new DateTime(now.Year, now.Month, 1);
                ViewData["PeriodLabel"] = "Tháng này";
                break;
        }

        // Lọc đơn hàng theo thời gian
        orders = orders.Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate);

        // Lấy đơn hàng đã hoàn thành (COMPLETED) cho thống kê doanh thu
        var completedOrders = orders.Where(o => o.Status == "COMPLETED");

        // Thống kê theo thời gian
        List<DailyReport> dailyReports = new List<DailyReport>();
        
        if (period?.ToLower() == "day")
        {
            // Thống kê theo giờ trong ngày
            dailyReports = completedOrders
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new DailyReport
                {
                    Date = new DateTime(now.Year, now.Month, now.Day, g.Key, 0, 0),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();
        }
        else if (period?.ToLower() == "week" || period?.ToLower() == "month")
        {
            // Thống kê theo ngày
            dailyReports = completedOrders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new DailyReport
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();
        }
        else if (period?.ToLower() == "year")
        {
            // Thống kê theo tháng
            dailyReports = completedOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new DailyReport
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToList();
        }

        // Sắp xếp theo yêu cầu (giảm dần)
        if (sortBy?.ToLower() == "orders")
        {
            dailyReports = dailyReports.OrderByDescending(r => r.OrderCount).ToList();
        }
        else
        {
            dailyReports = dailyReports.OrderByDescending(r => r.Revenue).ToList();
        }

        // Tổng hợp
        var totalOrders = completedOrders.Count();
        var totalRevenue = completedOrders.Sum(o => (decimal?)o.TotalAmount) ?? 0;
        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        ViewData["Period"] = period;
        ViewData["SortBy"] = sortBy;
        ViewData["DailyReports"] = dailyReports;
        ViewData["TotalOrders"] = totalOrders;
        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["AvgOrderValue"] = avgOrderValue;

        return View();
    }
}

