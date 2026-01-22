using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Repositories.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> UpdateStockAsync(Guid productId, int quantityChange);
}
