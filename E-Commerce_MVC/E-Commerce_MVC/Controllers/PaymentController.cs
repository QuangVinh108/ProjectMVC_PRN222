using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_MVC.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ShopDbContext _context; // ✅ THÊM DÒNG NÀY

        public PaymentController(
            IPaymentService paymentService,
            ShopDbContext context) // ✅ INJECT
        {
            _paymentService = paymentService;
            _context = context;
        }

        // ================== PAY ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pay(int id)
        {
            var order = _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            // ❗ Nếu chưa có Payment thì tạo Pending
            if (order.Payment == null)
            {
                order.Payment = new Payment
                {
                    OrderId = id,
                    PaymentMethod = "VNPAY",
                    Amount = order.TotalAmount,
                    Status = "Pending",
                    PaidAt = null
                };

                _context.SaveChanges();
            }

            var paymentDto = new PaymentDto
            {
                OrderId = id,
                Amount = order.TotalAmount,
                PaymentMethod = "VNPAY",
                Status = "Pending"
            };

            var paymentUrl = _paymentService.CreateVnPayUrl(paymentDto, HttpContext);
            return Redirect(paymentUrl);
        }

        // ================== VNPAY RETURN ==================
        public IActionResult VnPayReturn()
        {
            if (_paymentService.HandleVnPayReturn(Request.Query, out int orderId))
            {
                var order = _context.Orders
                    .Include(o => o.Payment)
                    .FirstOrDefault(o => o.OrderId == orderId);

                if (order != null && order.Payment != null)
                {
                    // ✅ CẬP NHẬT TRẠNG THÁI
                    order.Status = "Paid";
                    order.Payment.Status = "Paid";
                    order.Payment.PaidAt = DateTime.Now;

                    _context.SaveChanges();
                }

                TempData["Success"] = "Thanh toán VNPAY thành công!";
            }
            else
            {
                var txnRef = Request.Query["vnp_TxnRef"].ToString();
                orderId = int.Parse(txnRef.Split('_')[0]);

                var order = _context.Orders
                    .Include(o => o.Payment)
                    .FirstOrDefault(o => o.OrderId == orderId);

                if (order?.Payment != null)
                {
                    order.Payment.Status = "Failed";
                    _context.SaveChanges();
                }

                TempData["Error"] = "Thanh toán VNPAY thất bại!";
            }

            return RedirectToAction("Details", "Order", new { id = orderId });
        }
    }
}
