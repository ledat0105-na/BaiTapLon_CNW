using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("order_items")]
public class OrderDetail
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("order_id")]
    public long OrderId { get; set; }
    
    [Required]
    [Column("product_id")]
    public long ProductId { get; set; }
    
    [Required]
    [Column("product_name")]
    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [Column("quantity")]
    public int Quantity { get; set; }
    
    [Required]
    [Column("unit_price", TypeName = "decimal(15,2)")]
    public decimal UnitPrice { get; set; }
    
    [Required]
    [Column("line_total", TypeName = "decimal(15,2)")]
    public decimal LineTotal { get; set; }
    
    // Computed properties để tương thích với code hiện tại
    [NotMapped]
    public int OrderDetailId => (int)Id;
    
    [NotMapped]
    public decimal SubTotal => LineTotal;
    
    // Navigation properties
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
    
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;
}

