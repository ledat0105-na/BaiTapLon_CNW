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

    // Sản phẩm bán chạy nhất
    [Column("best_seller_product_id")]
    public long? BestSellerProductId { get; set; }
    
    [Column("best_seller_image_url")]
    [StringLength(500)]
    public string? BestSellerImageUrl { get; set; }
    
    [Column("best_seller_title")]
    [StringLength(200)]
    public string? BestSellerTitle { get; set; }
    
    [Column("best_seller_description")]
    [StringLength(500)]
    public string? BestSellerDescription { get; set; }

    // Sản phẩm mới
    [Column("new_arrival_product_id")]
    public long? NewArrivalProductId { get; set; }
    
    [Column("new_arrival_image_url")]
    [StringLength(500)]
    public string? NewArrivalImageUrl { get; set; }
    
    [Column("new_arrival_title")]
    [StringLength(200)]
    public string? NewArrivalTitle { get; set; }
    
    [Column("new_arrival_description")]
    [StringLength(500)]
    public string? NewArrivalDescription { get; set; }

    // Khuyến mãi
    [Column("special_offer_product_id")]
    public long? SpecialOfferProductId { get; set; }
    
    [Column("special_offer_image_url")]
    [StringLength(500)]
    public string? SpecialOfferImageUrl { get; set; }
    
    [Column("special_offer_title")]
    [StringLength(200)]
    public string? SpecialOfferTitle { get; set; }
    
    [Column("special_offer_description")]
    [StringLength(500)]
    public string? SpecialOfferDescription { get; set; }

    // Cài đặt banner
    [Column("banner_slide_interval")]
    public int BannerSlideInterval { get; set; } = 5000; // Thời gian chuyển slide (mili giây), mặc định 5 giây. 0 = không tự động chuyển

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey("BestSellerProductId")]
    public virtual Product? BestSellerProduct { get; set; }
    
    [ForeignKey("NewArrivalProductId")]
    public virtual Product? NewArrivalProduct { get; set; }
    
    [ForeignKey("SpecialOfferProductId")]
    public virtual Product? SpecialOfferProduct { get; set; }
}

