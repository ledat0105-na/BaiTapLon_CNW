using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Attributes;
using WebHoney.Data;
using WebHoney.Models;

namespace WebHoney.Controllers;

[Authorize("Admin", "ADMIN")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerController> _logger;
    private readonly IConfiguration _configuration;

    public CustomerController(ApplicationDbContext context, ILogger<CustomerController> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // GET: Customer - Danh sách khách hàng
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 5;
        var customers = from c in _context.Customers
                       select c;

        if (!string.IsNullOrEmpty(searchString))
        {
            customers = customers.Where(c => 
                c.FullName.Contains(searchString) ||
                (c.Email != null && c.Email.Contains(searchString)) ||
                c.Phone.Contains(searchString)
            );
        }

        customers = customers.OrderByDescending(c => c.CreatedAt);

        var totalItems = await customers.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

        var pagedCustomers = await customers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["SearchString"] = searchString;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalItems;
        ViewData["PageSize"] = pageSize;

        return View(pagedCustomers);
    }

    // GET: Customer/Details/5
    public async Task<IActionResult> Details(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // GET: Customer/Create
    public IActionResult Create()
    {
        ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
        return View();
    }

    // POST: Customer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Email,Phone,Address")] Customer customer)
    {
        if (ModelState.IsValid)
        {
            customer.CreatedAt = DateTime.Now;
            _context.Add(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm khách hàng thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    // GET: Customer/Edit/5
    public async Task<IActionResult> Edit(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    // POST: Customer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Id,FullName,Email,Phone,Address,CreatedAt")] Customer customer)
    {
        if (id != customer.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customer.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    // GET: Customer/Delete/5
    public async Task<IActionResult> Delete(long? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.Id == id);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    // POST: Customer/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa khách hàng thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Customer/ToggleActive/5 - Khóa/Mở khóa khách hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        customer.IsActive = !customer.IsActive;
        await _context.SaveChangesAsync();
        
        var message = customer.IsActive ? "Mở khóa khách hàng thành công!" : "Khóa khách hàng thành công!";
        TempData["SuccessMessage"] = message;
        
        return RedirectToAction(nameof(Index));
    }

    private bool CustomerExists(long id)
    {
        return _context.Customers.Any(e => e.Id == id);
    }
}

