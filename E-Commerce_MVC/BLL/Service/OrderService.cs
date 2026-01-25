using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;

namespace BLL.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;
        private readonly IInventoryService _inventoryService;

        public OrderService(IOrderRepository orderRepo, ICartRepository cartRepo, IInventoryService _inventoryService)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
            _inventoryService = _inventoryService;
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
                Note = dto.Note,
                IsActive = true
            };

            // 4. Create OrderItems from CartItems
            order.OrderItems = cart.CartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                Image = ci.Product?.Image
            }).ToList();

            // 5. Create Payment record
            order.Payment = new Payment
            {
                PaymentMethod = dto.PaymentMethod,
                Amount = totalAmount,
                Status = "Pending",
                PaidAt = null
            };

            // 6. Create Shipping record
            order.Shipping = new Shipping
            {
                Address = dto.ShippingAddress,
                City = dto.City,
                Country = dto.Country,
                PostalCode = dto.PostalCode
            };

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

            // If marking as Paid, update payment record as well (fake payment)
            if (newStatus == "Paid")
            {
                if (order.Payment == null)
                {
                    // create a payment if missing (defensive)
                    order.Payment = new Payment
                    {
                        PaymentMethod = "Unknown",
                        Amount = order.TotalAmount,
                        Status = "Paid",
                        PaidAt = DateTime.Now
                    };
                }
                else
                {
                    order.Payment.Status = "Paid";
                    order.Payment.PaidAt = DateTime.Now;
                }
            }

            order.Status = newStatus;
            await _orderRepo.UpdateAsync(order);
            return true;
        }

        //public async Task<bool> CancelOrderAsync(int orderId, int userId)
        //{
        //    var order = await _orderRepo.GetByIdAsync(orderId);
        //    if (order == null) return false;

        //    // Check ownership
        //    if (order.UserId != userId)
        //        throw new Exception("Bạn không có quyền hủy đơn hàng này");

        //    // Only allow cancellation if order is Pending
        //    if (order.Status != "Pending")
        //        throw new Exception("Chỉ có thể hủy đơn hàng ở trạng thái Pending");

        //    order.Status = "Cancelled";
        //    await _orderRepo.UpdateAsync(order);
        //    return true;
        //}

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return false;

            if (order.UserId != userId)
                throw new Exception("Bạn không có quyền hủy đơn hàng này");

            // chỉ cho phép Pending hoặc Paid
            if (order.Status != "Pending" && order.Status != "Paid")
                throw new Exception("Không thể hủy đơn hàng ở trạng thái hiện tại");

            var oldStatus = order.Status;

            order.Status = "Cancelled";
            await _orderRepo.UpdateAsync(order);

            // ✅ QUAN TRỌNG: chỉ hoàn kho nếu đơn ĐÃ TRỪ KHO TRƯỚC ĐÓ
            if (oldStatus == "Paid")
            {
                await _inventoryService.RestoreInventoryAsync(orderId);
            }

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
                Payment = order.Payment != null ? new PaymentDto
                {
                    PaymentId = order.Payment.PaymentId,
                    OrderId = order.OrderId,
                    PaymentMethod = order.Payment.PaymentMethod,
                    Amount = order.Payment.Amount,
                    PaidAt = order.Payment.PaidAt,
                    Status = order.Payment.Status
                } : null,
                Shipping = order.Shipping != null ? new ShippingDto
                {
                    ShippingId = order.Shipping.ShippingId,
                    OrderId = order.OrderId,
                    Address = order.Shipping.Address,
                    City = order.Shipping.City,
                    Country = order.Shipping.Country,
                    PostalCode = order.Shipping.PostalCode,
                    Carrier = order.Shipping.Carrier,
                    TrackingNumber = order.Shipping.TrackingNumber,
                    ShippedDate = order.Shipping.ShippedDate,
                    DeliveryDate = order.Shipping.DeliveryDate
                } : null
            };
        }


        //public async Task<GenericResult<bool>> CancelOrderAsync(int orderId, string cancelReason)
        //{
        //    var order = await _orderRepo.GetByIdAsync(orderId);
        //    if (order == null || order.Status != "Pending")
        //        return GenericResult<bool>.Failure("Cannot cancel this order");

        //    // 1. Cộng lại INVENTORY
        //    var inventoryResult = await _inventoryService.ProcessPaymentInventoryAsync(orderId, "Cancelled");
        //    if (!inventoryResult.IsSuccess)
        //        return GenericResult<bool>.Failure("Inventory restore failed");

        //    // 2. Update Order status
        //    order.Status = "Cancelled";
        //    order.Note += $"\nCancelled: {cancelReason}";
        //    await _orderRepo.UpdateAsync(order);

        //    return GenericResult<bool>.Success(true, "Order cancelled + stock restored");
        //}
    }
}

