using WebHoney.Models;

namespace WebHoney.Models;

public class AccountProfileViewModel
{
    public User User { get; set; } = new User();
    public List<Order> Orders { get; set; } = new();
}
