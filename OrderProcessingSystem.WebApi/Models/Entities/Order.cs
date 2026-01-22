using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.Entities;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Expired
}

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
