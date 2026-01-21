using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
