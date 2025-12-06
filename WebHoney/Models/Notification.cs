using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebHoney.Models;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Required]
    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    [StringLength(1000)]
    public string? Message { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "INFO"; // INFO, SUCCESS, WARNING, ERROR

    [Column("related_id")]
    public long? RelatedId { get; set; } // ID của đơn hàng hoặc entity liên quan

    [Column("related_type")]
    [StringLength(50)]
    public string? RelatedType { get; set; } // ORDER, USER, etc.

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

