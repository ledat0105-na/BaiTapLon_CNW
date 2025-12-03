using System.Text.Json;
using WebHoney.Models;

namespace WebHoney.Services;

public interface ICartService
{
    Dictionary<long, CartItem> GetCart(ISession session);
    void AddToCart(ISession session, long productId, int quantity, Product product);
    void UpdateQuantity(ISession session, long productId, int quantity);
    void RemoveFromCart(ISession session, long productId);
    void ClearCart(ISession session);
    int GetCartItemCount(ISession session);
    decimal GetCartTotal(ISession session);
}

public class CartService : ICartService
{
    private const string CartSessionKey = "Cart";

    public Dictionary<long, CartItem> GetCart(ISession session)
    {
        var cartJson = session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new Dictionary<long, CartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<long, CartItem>>(cartJson) ?? new Dictionary<long, CartItem>();
        }
        catch
        {
            return new Dictionary<long, CartItem>();
        }
    }

    public void AddToCart(ISession session, long productId, int quantity, Product product)
    {
        var cart = GetCart(session);
        
        if (cart.ContainsKey(productId))
        {
            cart[productId].Quantity += quantity;
            // Đảm bảo không vượt quá stock
            if (cart[productId].Quantity > product.Stock)
            {
                cart[productId].Quantity = product.Stock;
            }
        }
        else
        {
            cart[productId] = new CartItem
            {
                ProductId = productId,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = quantity > product.Stock ? product.Stock : quantity,
                Stock = product.Stock
            };
        }

        SaveCart(session, cart);
    }

    public void UpdateQuantity(ISession session, long productId, int quantity)
    {
        var cart = GetCart(session);
        
        if (cart.ContainsKey(productId))
        {
            if (quantity <= 0)
            {
                cart.Remove(productId);
            }
            else
            {
                // Đảm bảo không vượt quá stock
                if (quantity > cart[productId].Stock)
                {
                    quantity = cart[productId].Stock;
                }
                cart[productId].Quantity = quantity;
            }
            
            SaveCart(session, cart);
        }
    }

    public void RemoveFromCart(ISession session, long productId)
    {
        var cart = GetCart(session);
        cart.Remove(productId);
        SaveCart(session, cart);
    }

    public void ClearCart(ISession session)
    {
        session.Remove(CartSessionKey);
    }

    public int GetCartItemCount(ISession session)
    {
        var cart = GetCart(session);
        return cart.Values.Sum(item => item.Quantity);
    }

    public decimal GetCartTotal(ISession session)
    {
        var cart = GetCart(session);
        return cart.Values.Sum(item => item.Total);
    }

    private void SaveCart(ISession session, Dictionary<long, CartItem> cart)
    {
        var cartJson = JsonSerializer.Serialize(cart);
        session.SetString(CartSessionKey, cartJson);
    }
}

