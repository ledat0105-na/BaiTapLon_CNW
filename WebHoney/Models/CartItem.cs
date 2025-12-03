namespace WebHoney.Models;

public class CartItem
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    
    public decimal Total => Price * Quantity;
}

