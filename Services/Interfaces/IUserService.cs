using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;

namespace OrderProcessingSystem.Services.Interfaces;

public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserResponse>> GetAllAsync();
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeleteAsync(Guid id);
}
