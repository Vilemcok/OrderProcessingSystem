using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;

namespace OrderProcessingSystem.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<OrderResponse>> GetAllAsync();
    Task<IEnumerable<OrderResponse>> GetByUserIdAsync(Guid userId);
    Task<OrderResponse?> CreateOrderAsync(Guid userId, CreateOrderRequest request);
    Task<bool> DeleteAsync(Guid id);
}
