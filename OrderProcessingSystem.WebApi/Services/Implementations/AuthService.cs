using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;
using OrderProcessingSystem.Models.Entities;
using OrderProcessingSystem.Repositories.Interfaces;
using OrderProcessingSystem.Services.Interfaces;

namespace OrderProcessingSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) return null;

        if (user.Password != request.Password)
            return null;

        var token = _tokenService.GenerateToken(user);
        var expirationHours = 1; // Should match configuration
        var expiration = DateTime.UtcNow.AddHours(expirationHours);

        return new LoginResponse
        {
            Token = token,
            Expiration = expiration,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }
}
