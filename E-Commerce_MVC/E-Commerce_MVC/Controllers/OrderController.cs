using BLL.DTOs;
using BLL.IService;
using DAL.IRepository;
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

        // ================== INDEX ==================
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return View(orders);
        }

        // ================== DETAILS ==================
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id, GetCurrentUserId());
            return order == null ? RedirectToAction(nameof(Index)) : View(order);
        }

        // ================== CHECKOUT ==================
        public IActionResult Checkout()
        {
            return View(new CreateOrderDto
            {
                PaymentMethod = "COD",
                Country = "Vietnam"
            });
        }

        // ================== CREATE ORDER ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return View("Checkout", dto);

            dto.UserId = GetCurrentUserId();
            var order = await _orderService.CreateOrderAsync(dto);

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }

        // ================== BUY NOW ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                var dto = new CreateOrderDto { UserId = userId, PaymentMethod = "COD", Country = "Vietnam" };

                var order = await _orderService.CreateOrderBuyNowAsync(userId, productId, quantity, dto);
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "Shop");
            }
        }

        // ================== HELPER ==================
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Vui lòng đăng nhập");

            return int.Parse(userIdClaim);
        }

        // ================== CANCEL ORDER ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var result = await _orderService.CancelOrderAsync(id, userId);

                if (result)
                {
                    TempData["Success"] = "Hủy đơn hàng thành công";
                }
                else
                {
                    TempData["Error"] = "Không thể hủy đơn hàng";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
