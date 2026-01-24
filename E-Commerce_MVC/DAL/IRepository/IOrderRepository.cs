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

        //Dashboard
        Task<int> GetTotalOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Order>> GetOrdersThisMonthAsync();
        Task<List<Order>> GetOrdersLastMonthAsync();
        Task<Dictionary<string, int>> GetOrdersByStatusAsync();
        Task<List<Order>> GetRecentOrdersAsync(int count = 10);

    }
}

