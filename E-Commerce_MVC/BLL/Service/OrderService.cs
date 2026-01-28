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

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId, includeDetails: true);
            // Check quyền sở hữu
            if (order == null || order.UserId != userId) return null;
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
            var cart = await _cartRepo.GetByUserIdAsync(dto.UserId);
            if (cart == null || !cart.CartItems.Any()) throw new Exception("Giỏ hàng trống.");

            decimal totalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);

            var order = new Order
            {
                UserId = dto.UserId,
                OrderDate = DateTime.Now,
                Status = "Pending", // Hardcode string
                TotalAmount = totalAmount,
                Note = dto.Note,
                IsActive = true,
                OrderItems = cart.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    // Image = ci.Product.Image
                }).ToList(),
                Payment = new Payment
                {
                    PaymentMethod = dto.PaymentMethod,
                    Amount = totalAmount,
                    Status = "Pending" // Hardcode string
                },
                Shipping = new Shipping
                {
                    Address = dto.ShippingAddress,
                    City = dto.City,
                    Country = dto.Country,
                    PostalCode = dto.PostalCode
                }
            };

            var createdOrder = await _orderRepo.CreateAsync(order);
            await _cartRepo.ClearCartAsync(dto.UserId);

            return MapToOrderDto(await _orderRepo.GetByIdAsync(createdOrder.OrderId, true)!);
        }
        public async Task<OrderDto> CreateOrderBuyNowAsync(int userId, int productId, int quantity, CreateOrderDto dto)
        {
            await _cartRepo.AddOrReplaceSingleItemAsync(userId, productId, quantity);

            dto.UserId = userId;
            return await CreateOrderAsync(dto);
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
            if (order == null || order.UserId != userId) return false;

            // Chỉ cho hủy khi Pending hoặc Paid
            if (order.Status != "Pending" && order.Status != "Paid")
                throw new Exception("Không thể hủy đơn hàng này.");

            var oldStatus = order.Status;
            order.Status = "Cancelled"; // Hardcode string
            await _orderRepo.UpdateAsync(order);

            // Hoàn kho nếu đã thanh toán
            if (oldStatus == "Paid")
            {
                await _inventoryService.RestoreInventoryAsync(orderId);
            }
            return true;
        }



        // Helper Map
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                FullName = order.User?.FullName ?? "",
                UserName = order.User?.UserName ?? "",
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
                    PostalCode = order.Shipping.PostalCode
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

