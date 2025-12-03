using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("category_id")]
    public long CategoryId { get; set; }
    
    [Required]
    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;
    
    [Column("slug")]
    [StringLength(180)]
    public string Slug { get; set; } = string.Empty;
    
    [Column("short_desc")]
    [StringLength(255)]
    public string? ShortDesc { get; set; }
    
    [Column("description", TypeName = "TEXT")]
    public string? Description { get; set; }
    
    [Column("origin")]
    [StringLength(150)]
    public string? Origin { get; set; }
    
    [Column("volume", TypeName = "decimal(10,2)")]
    public decimal? Volume { get; set; }
    
    [Column("unit")]
    [StringLength(50)]
    public string? Unit { get; set; }
    
    [Required]
    [Column("price", TypeName = "decimal(15,2)")]
    public decimal Price { get; set; }
    
    [Column("image_url")]
    [StringLength(255)]
    public string? ImageUrl { get; set; }
    
    [Column("stock_quantity")]
    public int Stock { get; set; } = 0;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Computed properties để tương thích với code hiện tại
    [NotMapped]
    public int ProductId => (int)Id;
    
    [NotMapped]
    public string ProductName => Name;
    
    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = null!;
    
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

