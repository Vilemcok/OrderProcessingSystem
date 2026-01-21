namespace OrderProcessingSystem.Models.DTOs.Responses;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UserResponse User { get; set; } = null!;
}
