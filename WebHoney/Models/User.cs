using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("users")] // Map với bảng 'users' trong database (chữ thường)
public class User
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    
    [StringLength(100)]
    [Column("username")]
    public string? Username { get; set; }
    
    [Required]
    [StringLength(150)]
    [Column("email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [StringLength(255)]
    [Column("password_hash")]
    public string? PasswordHash { get; set; }
    
    [StringLength(255)]
    [Column("external_provider")]
    public string? ExternalProvider { get; set; } // "Google", "Facebook"
    
    [StringLength(255)]
    [Column("external_id")]
    public string? ExternalId { get; set; } // ID từ Google/Facebook
    
    [Required]
    [StringLength(150)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [StringLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }
    
    [Required]
    [Column("role")]
    public string Role { get; set; } = "CUSTOMER"; // ADMIN, CUSTOMER (theo ENUM trong DB)
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
    
    // Property để tương thích với code hiện tại (không lưu vào DB)
    [NotMapped]
    public long UserId => Id;
}

