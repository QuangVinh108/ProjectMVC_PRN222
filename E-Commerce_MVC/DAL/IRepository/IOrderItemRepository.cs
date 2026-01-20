using DAL.Entities;

namespace DAL.IRepository
{
    public interface IOrderItemRepository
    {
        Task<List<OrderItem>> GetByOrderIdAsync(int orderId);
        Task<OrderItem> CreateAsync(OrderItem orderItem);
        Task CreateRangeAsync(List<OrderItem> orderItems);
    }
}

