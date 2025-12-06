namespace WebHoney.ViewModels;

public class LockAccountViewModel
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? LockReason { get; set; }
}

