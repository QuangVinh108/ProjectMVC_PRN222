using DAL.Entities;

namespace DAL.IRepository
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int orderId, bool includeDetails = false);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task<List<Order>> GetAllAsync();
        Task<Order> CreateAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int orderId);
        Task<bool> ExistsAsync(int orderId);
    }
}

