using OrderProcessingSystem.Models.DTOs.Requests;
using OrderProcessingSystem.Models.DTOs.Responses;

namespace OrderProcessingSystem.Services.Interfaces;

public interface IProductService
{
    Task<ProductResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProductResponse>> GetAllAsync();
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeleteAsync(Guid id);
}
