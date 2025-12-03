using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebHoney.Data;
using WebHoney.Models;
using WebHoney.Services;

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

    // GET: Cart - Xem giỏ hàng
    public IActionResult Index()
    {
        var cart = _cartService.GetCart(HttpContext.Session);
        ViewData["CartTotal"] = _cartService.GetCartTotal(HttpContext.Session);
        return View(cart.Values.ToList());
    }

    // POST: Cart/Add - Thêm sản phẩm vào giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
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

