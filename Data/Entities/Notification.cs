using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YessGoFront.Data.Entities;

/// <summary>
/// Тип уведомления
/// </summary>
public enum NotificationType
{
    Push,
    Sms,
    Email,
    InApp
}

/// <summary>
/// Статус уведомления
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Delivered,
    Read
}

/// <summary>
/// Приоритет уведомления
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Уведомление
/// </summary>
[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message", TypeName = "text")]
    public string Message { get; set; } = string.Empty;

    [Required]
    [Column("notification_type")]
    public NotificationType NotificationType { get; set; }

    [Column("priority")]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    [Column("status")]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [Column("data", TypeName = "jsonb")]
    public Dictionary<string, object>? Data { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("delivered_at")]
    public DateTime? DeliveredAt { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    // Helper properties for UI
    [NotMapped]
    public string Icon => GetNotificationIcon();

    [NotMapped]
    public bool IsRead => ReadAt.HasValue;

    private string GetNotificationIcon()
    {
        return NotificationType switch
        {
            NotificationType.Push => "🔔",
            NotificationType.Sms => "💬",
            NotificationType.Email => "✉️",
            NotificationType.InApp => "ℹ️",
            _ => "🔔"
        };
    }
}

