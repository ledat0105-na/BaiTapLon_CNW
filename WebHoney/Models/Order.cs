using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("orders")]
public class Order
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("customer_id")]
    public long? CustomerId { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Required]
    [Column("full_name")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [Column("total_amount", TypeName = "decimal(15,2)")]
    public decimal TotalAmount { get; set; }
    
    [Column("address")]
    [StringLength(255)]
    public string? ShippingAddress { get; set; }
    
    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "PENDING"; // PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELED
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Computed property để tương thích với code hiện tại
    [NotMapped]
    public int OrderId => (int)Id;
    
    [NotMapped]
    public DateTime OrderDate => CreatedAt;
    
    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
    
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

