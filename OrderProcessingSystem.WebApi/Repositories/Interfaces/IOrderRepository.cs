using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);
}
