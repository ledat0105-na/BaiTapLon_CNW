using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("user_id")]
    public long? UserId { get; set; }
    
    [Required]
    [Column("full_name")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;
    
    [Column("email")]
    [StringLength(150)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [Required]
    [Column("phone")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [Column("address")]
    [StringLength(255)]
    public string? Address { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Computed property để tương thích với code hiện tại
    [NotMapped]
    public int CustomerId => (int)Id;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

