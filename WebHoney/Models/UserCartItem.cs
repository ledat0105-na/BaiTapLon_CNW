using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("user_cart_items")]
public class UserCartItem
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("user_id")]
    public long UserId { get; set; }

    [Required]
    [Column("product_id")]
    public long ProductId { get; set; }

    [Required]
    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
