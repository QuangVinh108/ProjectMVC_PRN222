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
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
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
            // Kiểm tra xem có phải AJAX request không
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            try
            {
                // Kiểm tra authentication
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("BuyNow: User not authenticated");

                    if (isAjaxRequest)
                    {
                        Response.StatusCode = 401;
                        return Json(new
                        {
                            success = false,
                            requireLogin = true,
                            message = "Vui lòng đăng nhập để tiếp tục"
                        });
                    }

                    return RedirectToAction("Login", "Account", new { returnUrl = Request.Path });
                }

                var userId = GetCurrentUserId();

                _logger.LogInformation($"BuyNow: User {userId}, Product {productId}, Quantity {quantity}");

                var dto = new CreateOrderDto
                {
                    UserId = userId,
                    PaymentMethod = "COD",
                    Country = "Vietnam"
                };

                var order = await _orderService.CreateOrderBuyNowAsync(userId, productId, quantity, dto);

                _logger.LogInformation($"BuyNow: Order created successfully - OrderId {order.OrderId}");

                // Nếu là AJAX request, return JSON với redirect URL
                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action(nameof(Details), new { id = order.OrderId }),
                        message = "Đặt hàng thành công"
                    });
                }

                // Nếu là normal form submit, redirect như bình thường
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BuyNow error: {ex.Message}");

                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }

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
