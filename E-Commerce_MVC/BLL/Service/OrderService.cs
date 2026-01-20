using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;

namespace BLL.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;

        public OrderService(IOrderRepository orderRepo, ICartRepository cartRepo)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            if (order == null) return null;

            return MapToOrderDto(order);
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepo.GetByUserIdAsync(userId);
            return orders.Select(MapToOrderDto).ToList();
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepo.GetAllAsync();
            return orders.Select(MapToOrderDto).ToList();
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            // 1. Get user's cart
            var cart = await _cartRepo.GetByUserIdAsync(dto.UserId);
            if (cart == null || !cart.CartItems.Any())
                throw new Exception("Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi đặt hàng.");

            // 2. Calculate total
            decimal totalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);

            // 3. Create Order
            var order = new Order
            {
                UserId = dto.UserId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = totalAmount,
                Note = dto.Note
            };

            // 4. Create OrderItems from CartItems
            order.OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            }).ToList();

            // 5. Create Payment record
            order.Payments.Add(new Payment
            {
                PaymentMethod = dto.PaymentMethod,
                Amount = totalAmount,
                Status = "Pending",
                PaidAt = null
            });

            // 6. Create Shipping record
            order.Shippings.Add(new Shipping
            {
                Address = dto.ShippingAddress,
                City = dto.City,
                Country = dto.Country,
                PostalCode = dto.PostalCode
            });

            // 7. Save Order
            var createdOrder = await _orderRepo.CreateAsync(order);

            // 8. Clear Cart
            await _cartRepo.ClearCartAsync(dto.UserId);

            // 9. Return OrderDto
            var result = await _orderRepo.GetByIdAsync(createdOrder.OrderId, includeDetails: true);
            return MapToOrderDto(result!);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return false;

            // Validate status transitions
            var validStatuses = new[] { "Pending", "Paid", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new Exception("Trạng thái không hợp lệ");

            order.Status = newStatus;
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return false;

            // Check ownership
            if (order.UserId != userId)
                throw new Exception("Bạn không có quyền hủy đơn hàng này");

            // Only allow cancellation if order is Pending
            if (order.Status != "Pending")
                throw new Exception("Chỉ có thể hủy đơn hàng ở trạng thái Pending");

            order.Status = "Cancelled";
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        // Helper method to map Order entity to OrderDto
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                UserName = order.User?.UserName ?? "",
                FullName = order.User?.FullName ?? "",
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Note = order.Note,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.ProductName ?? "",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList(),
                Payment = order.Payments.FirstOrDefault() != null ? new PaymentDto
                {
                    PaymentId = order.Payments.First().PaymentId,
                    OrderId = order.OrderId,
                    PaymentMethod = order.Payments.First().PaymentMethod,
                    Amount = order.Payments.First().Amount,
                    PaidAt = order.Payments.First().PaidAt,
                    Status = order.Payments.First().Status
                } : null,
                Shipping = order.Shippings.FirstOrDefault() != null ? new ShippingDto
                {
                    ShippingId = order.Shippings.First().ShippingId,
                    OrderId = order.OrderId,
                    Address = order.Shippings.First().Address,
                    City = order.Shippings.First().City,
                    Country = order.Shippings.First().Country,
                    PostalCode = order.Shippings.First().PostalCode,
                    Carrier = order.Shippings.First().Carrier,
                    TrackingNumber = order.Shippings.First().TrackingNumber,
                    ShippedDate = order.Shippings.First().ShippedDate,
                    DeliveryDate = order.Shippings.First().DeliveryDate
                } : null
            };
        }
    }
}

