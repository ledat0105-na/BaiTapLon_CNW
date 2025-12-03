using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("banner_images")]
public class BannerImage
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("image_url")]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;
    
    [Column("title")]
    [StringLength(200)]
    public string? Title { get; set; }
    
    [Column("subtitle")]
    [StringLength(200)]
    public string? Subtitle { get; set; }
    
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

