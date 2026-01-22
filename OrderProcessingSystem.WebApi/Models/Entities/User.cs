using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
