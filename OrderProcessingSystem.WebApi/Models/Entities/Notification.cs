using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.Entities;

public enum NotificationType
{
    Completed,
    Expired
}

public class Notification
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Order Order { get; set; } = null!;
}
