using System.ComponentModel.DataAnnotations;

namespace OrderProcessingSystem.Models.DTOs.Requests;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MinLength(6)]
    public string? Password { get; set; }
}
