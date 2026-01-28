using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;

        public PaymentController(IPaymentService paymentService, IOrderService orderService)
        {
            _paymentService = paymentService;
            _orderService = orderService;
        }

        // ================== PAY ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null) return NotFound();

            // 1. Tạo Payment Pending (Gọi Service)
            await _paymentService.CreatePendingPaymentAsync(id, order.TotalAmount);

            // 2. Tạo URL và Redirect
            var paymentDto = new PaymentDto
            {
                OrderId = id,
                Amount = order.TotalAmount,
                PaymentMethod = "VNPAY"
            };

            var url = _paymentService.CreateVnPayUrl(paymentDto, HttpContext);

            return Redirect(url);
        }

        // ================== VNPAY RETURN ==================
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = await _paymentService.ProcessVnPayReturnAsync(Request.Query);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            if (result.OrderId > 0)
                return RedirectToAction("Details", "Order", new { id = result.OrderId });

            return RedirectToAction("Index", "Order");
        }

    }
}
