using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.Services;
using WebHoney.Extensions;

namespace WebHoney.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ApplicationDbContext context, ICartService cartService, ILogger<CartController> logger)
    {
        _context = context;
        _cartService = cartService;
        _logger = logger;
    }

    private bool IsUserLoggedIn() => !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));

    // GET: Cart - Xem giỏ hàng
    public IActionResult Index()
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
        }

        var cart = _cartService.GetCart(HttpContext.Session);
        ViewData["CartTotal"] = _cartService.GetCartTotal(HttpContext.Session);
        return View(cart.Values.ToList());
    }

    // POST: Cart/Add - Thêm sản phẩm vào giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        if (!IsUserLoggedIn())
        {
            return Json(new { success = false, requiresLogin = true, message = "Vui lòng đăng nhập trước khi thêm sản phẩm vào giỏ hàng." });
        }

        try
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại!" });
            }

            if (product.Stock <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm đã hết hàng!" });
            }

            if (request.Quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ!" });
            }

            var currentCart = _cartService.GetCart(HttpContext.Session);
            var currentQuantity = currentCart.ContainsKey(request.ProductId) 
                ? currentCart[request.ProductId].Quantity 
                : 0;

            if (currentQuantity + request.Quantity > product.Stock)
            {
                return Json(new { 
                    success = false, 
                    message = $"Chỉ còn {product.Stock} sản phẩm trong kho. Bạn đã có {currentQuantity} trong giỏ hàng." 
                });
            }

            _cartService.AddToCart(HttpContext.Session, request.ProductId, request.Quantity, product);

            return Json(new { 
                success = true, 
                message = "Đã thêm sản phẩm vào giỏ hàng!",
                cartCount = _cartService.GetCartItemCount(HttpContext.Session)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product to cart");
            return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng." });
        }
    }

    // POST: Cart/Update - Cập nhật số lượng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update([FromBody] UpdateCartRequest request)
    {
        if (!IsUserLoggedIn())
        {
            return Json(new { success = false, requiresLogin = true, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        }
        try
        {
            if (request.Quantity <= 0)
            {
                _cartService.RemoveFromCart(HttpContext.Session, request.ProductId);
            }
            else
            {
                _cartService.UpdateQuantity(HttpContext.Session, request.ProductId, request.Quantity);
            }

            var cart = _cartService.GetCart(HttpContext.Session);
            var item = cart.ContainsKey(request.ProductId) ? cart[request.ProductId] : null;
            
            return Json(new { 
                success = true,
                cartTotal = _cartService.GetCartTotal(HttpContext.Session),
                cartCount = _cartService.GetCartItemCount(HttpContext.Session),
                itemTotal = item?.Total ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart");
            return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật giỏ hàng." });
        }
    }

    // POST: Cart/Remove - Xóa sản phẩm khỏi giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove([FromBody] RemoveFromCartRequest request)
    {
        if (!IsUserLoggedIn())
        {
            return Json(new { success = false, requiresLogin = true, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        }
        try
        {
            _cartService.RemoveFromCart(HttpContext.Session, request.ProductId);
            
            return Json(new { 
                success = true,
                cartTotal = _cartService.GetCartTotal(HttpContext.Session),
                cartCount = _cartService.GetCartItemCount(HttpContext.Session)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cart");
            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm khỏi giỏ hàng." });
        }
    }

    // POST: Cart/Clear - Xóa toàn bộ giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        if (!IsUserLoggedIn())
        {
            return Json(new { success = false, requiresLogin = true, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        }
        try
        {
            _cartService.ClearCart(HttpContext.Session);
            return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa giỏ hàng." });
        }
    }

    // GET: Cart/Checkout - Trang thanh toán
    [HttpGet]
    public IActionResult Checkout()
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
        }
        var cart = _cartService.GetCart(HttpContext.Session);
        if (cart == null || !cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống, không thể thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["CartTotal"] = _cartService.GetCartTotal(HttpContext.Session);
        return View(cart.Values.ToList());
    }

    // POST: Cart/Checkout - Xác nhận thanh toán và tạo đơn hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckoutConfirm(string customerName, string phone, string address)
    {
        if (!IsUserLoggedIn())
        {
            TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
        }
        var cart = _cartService.GetCart(HttpContext.Session);
        if (cart == null || !cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống, không thể thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin khách hàng.";
            return RedirectToAction(nameof(Checkout));
        }

        // Tính tổng tiền
        var totalAmount = _cartService.GetCartTotal(HttpContext.Session);

        // Tìm hoặc tạo khách hàng theo số điện thoại
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = customerName.Trim(),
                Phone = phone.Trim(),
                Address = address.Trim(),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Cập nhật lại thông tin địa chỉ nếu thay đổi
            customer.FullName = customerName.Trim();
            customer.Address = address.Trim();
            await _context.SaveChangesAsync();
        }

        // Tạo đơn hàng
        var userId = HttpContext.Session.GetUserId();
        var order = new Order
        {
            CustomerId = customer.Id,
            UserId = userId,
            FullName = customerName.Trim(),
            TotalAmount = totalAmount,
            ShippingAddress = address.Trim(),
            Phone = phone.Trim(),
            Status = "PENDING",
            CreatedAt = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Tạo chi tiết đơn hàng
        foreach (var item in cart.Values)
        {
            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                LineTotal = item.Price * item.Quantity
            };

            _context.OrderDetails.Add(orderDetail);
        }

        await _context.SaveChangesAsync();

        // Xóa giỏ hàng sau khi tạo đơn
        _cartService.ClearCart(HttpContext.Session);

        // Điều hướng sang trang xác nhận đơn hàng
        return RedirectToAction(nameof(CheckoutSuccess), new { orderId = order.Id });
    }

    // GET: Cart/CheckoutSuccess - Trang xác nhận sau khi đặt hàng thành công
    [HttpGet]
    public async Task<IActionResult> CheckoutSuccess(long orderId)
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(CheckoutSuccess), "Cart", new { orderId }) });
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        return View(order);
    }

    // GET: Cart/MyOrders - Danh sách đơn hàng của khách hàng hiện tại
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(MyOrders), "Cart") });
        }

        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Không xác định được tài khoản khách hàng.";
            return RedirectToAction("Index", "Home");
        }

        var orders = await _context.Orders
            .Where(o => o.UserId == userId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    // GET: Cart/MyOrderDetails/5 - Chi tiết một đơn hàng của khách hiện tại
    [HttpGet]
    public async Task<IActionResult> MyOrderDetails(long id)
    {
        if (!IsUserLoggedIn())
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(MyOrderDetails), "Cart", new { id }) });
        }

        var userId = HttpContext.Session.GetUserId();
        if (!userId.HasValue)
        {
            TempData["ErrorMessage"] = "Không xác định được tài khoản khách hàng.";
            return RedirectToAction("Index", "Home");
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.";
            return RedirectToAction(nameof(MyOrders));
        }

        return View(order);
    }

    // GET: Cart/Count - Lấy số lượng sản phẩm trong giỏ hàng (cho AJAX)
    [HttpGet]
    public IActionResult Count()
    {
        var count = _cartService.GetCartItemCount(HttpContext.Session);
        return Json(new { count });
    }
}

// Request models
public class AddToCartRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateCartRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveFromCartRequest
{
    public long ProductId { get; set; }
}

