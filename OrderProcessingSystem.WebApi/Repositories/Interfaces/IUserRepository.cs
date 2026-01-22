using OrderProcessingSystem.Models.Entities;

namespace OrderProcessingSystem.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
