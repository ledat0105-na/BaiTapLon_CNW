namespace WebHoney.Models.ViewModels;

public class TopProductViewModel
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}
