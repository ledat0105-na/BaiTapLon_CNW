using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("featured_products")]
public class FeaturedProduct
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("product_id")]
    public long? ProductId { get; set; }
    
    [Column("image_url")]
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    
    [Column("title")]
    [StringLength(200)]
    public string? Title { get; set; }
    
    [Column("subtitle")]
    [StringLength(200)]
    public string? Subtitle { get; set; }
    
    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}

