using BLL.DTOs;
using BLL.Helper;

namespace BLL.IService
{
    public interface IOrderService
    {
        Task<OrderDto?> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderDto>> GetUserOrdersAsync(int userId);
        Task<List<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto> CreateOrderBuyNowAsync(int userId, int productId, int quantity, CreateOrderDto dto);
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        //Task<GenericResult<bool>> CancelOrderAsync(int orderId, string cancelReason);
    }
}

