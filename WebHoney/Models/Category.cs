using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Required]
    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Column("slug")]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;
    
    [Column("description", TypeName = "TEXT")]
    public string? Description { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    // Computed property để tương thích với code hiện tại
    [NotMapped]
    public int CategoryId => (int)Id;
    
    [NotMapped]
    public string CategoryName => Name;
    
    // Navigation property
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

