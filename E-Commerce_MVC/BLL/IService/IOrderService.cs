using BLL.DTOs;
using BLL.Helper;

namespace BLL.IService
{
    public interface IOrderService
    {
        Task<OrderDto?> GetOrderByIdAsync(int orderId);
        Task<List<OrderDto>> GetUserOrdersAsync(int userId);
        Task<List<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        //Task<GenericResult<bool>> CancelOrderAsync(int orderId, string cancelReason);
    }
}

