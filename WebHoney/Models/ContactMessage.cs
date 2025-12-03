using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("contact_messages")]
public class ContactMessage
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên")]
    [Column("name")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("subject")]
    [StringLength(200)]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
    [Column("message", TypeName = "TEXT")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "NEW"; // NEW, IN_PROGRESS, RESOLVED

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public User? User { get; set; }
}

