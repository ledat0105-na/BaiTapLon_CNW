using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("home_page_settings")]
public class HomePageSettings
{
    [Key]
    [Column("id")]
    public int Id { get; set; } = 1; // Chỉ có 1 record

    [Column("featured_image_url")]
    [StringLength(500)]
    public string? FeaturedImageUrl { get; set; } // Ảnh bên trái phần giới thiệu

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

