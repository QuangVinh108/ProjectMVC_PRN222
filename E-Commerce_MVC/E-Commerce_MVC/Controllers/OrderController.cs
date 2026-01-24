using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<OrderDto>());
            }
        }

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }

                // Check ownership
                var userId = GetCurrentUserId();
                if (order.UserId != userId)
                {
                    TempData["Error"] = "Bạn không có quyền xem đơn hàng này";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Order/Checkout
        public IActionResult Checkout()
        {
            return View(new CreateOrderDto
            {
                PaymentMethod = "COD",
                Country = "Vietnam"
            });
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Checkout", dto);
                }

                dto.UserId = GetCurrentUserId();
                var order = await _orderService.CreateOrderAsync(dto);

                TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng: #{order.OrderId}";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View("Checkout", dto);
            }
        }

        // POST: /Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _orderService.CancelOrderAsync(id, userId);

                if (result)
                    TempData["Success"] = "Đơn hàng đã được hủy thành công";
                else
                    TempData["Error"] = "Không thể hủy đơn hàng";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: /Order/Pay/5
        // Payment action for customers to mark an order as Paid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int id)
        {
            try
            {
                // Ensure order exists and belongs to current user
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();
                if (order.UserId != userId)
                {
                    TempData["Error"] = "Bạn không có quyền thao tác trên đơn hàng này";
                    return RedirectToAction(nameof(Index));
                }

                // Only allow paying when pending
                if (order.Status != "Pending")
                {
                    TempData["Error"] = "Chỉ có thể thanh toán đơn hàng ở trạng thái Chờ xử lý";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var result = await _orderService.UpdateOrderStatusAsync(id, "Paid");
                if (result)
                    TempData["Success"] = "Đã thanh toán thành công";
                else
                    TempData["Error"] = "Không thể cập nhật trạng thái thanh toán";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Vui lòng đăng nhập để tiếp tục");
            
            return int.Parse(userIdClaim);
        }
    }
}

