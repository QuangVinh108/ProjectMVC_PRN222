using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pay(int id)
        {
            // TODO: Lấy Order từ DB
            var totalAmount = 100000; // ví dụ

            var payment = new PaymentDto
            {
                OrderId = id,
                Amount = totalAmount,
                PaymentMethod = "VNPAY",
                Status = "Pending"
            };

            var paymentUrl = _paymentService.CreateVnPayUrl(payment, HttpContext);

            // TODO: Save Payment (Status = Pending) vào DB

            return Redirect(paymentUrl);
        }

        public IActionResult VnPayReturn()
        {
            if (_paymentService.HandleVnPayReturn(Request.Query, out int orderId))
            {
                // TODO:
                // Update Payment.Status = Paid
                // Payment.PaidAt = DateTime.Now
                // Order.Status = Paid

                TempData["Success"] = "Thanh toán VNPAY thành công!";
            }
            else
            {
                // TODO: Update Payment.Status = Failed
                TempData["Error"] = "Thanh toán VNPAY thất bại!";
            }

            return RedirectToAction("Details", "Order", new { id = orderId });
        }
    }
}
