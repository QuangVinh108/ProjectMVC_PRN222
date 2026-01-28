using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using DAL.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BLL.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(
            IInventoryService inventoryService,
            IConfiguration config,
            ILogger<PaymentService> logger,
            IOrderRepository orderRepository)
        {
            _inventoryService = inventoryService;
            _config = config;
            _logger = logger;
            _orderRepository = orderRepository;
        }

        public async Task CreatePendingPaymentAsync(int orderId, decimal amount)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, includeDetails: true);
            if (order == null) throw new Exception("Order not found");

            if (order.Payment == null)
            {
                order.Payment = new Payment
                {
                    OrderId = orderId,
                    PaymentMethod = "VNPAY", // Hardcode string
                    Amount = amount,
                    Status = "Pending",      // Hardcode string
                    PaidAt = null
                };
                await _orderRepository.UpdateAsync(order);
            }
        }

        public string CreateVnPayUrl(PaymentDto payment, HttpContext context)
        {
            var vnpay = new SortedDictionary<string, string>
    {
        { "vnp_Version", "2.1.0" },
        { "vnp_Command", "pay" },
        { "vnp_TmnCode", _config["VnPay:TmnCode"] },
        { "vnp_Amount", ((long)(payment.Amount * 100)).ToString() },
        { "vnp_CurrCode", "VND" },
        { "vnp_TxnRef", payment.OrderId.ToString() },
        { "vnp_OrderInfo", $"Thanh_toan_don_hang_{payment.OrderId}" },
        { "vnp_OrderType", "other" },
        { "vnp_Locale", "vn" },
        { "vnp_ReturnUrl", _config["VnPay:ReturnUrl"] },
        { "vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" },
        { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
        { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
    };

            // ✅ 1. HASH DATA (KHÔNG URL ENCODE)
            var hashData = string.Join("&",
    vnpay.Select(kv =>
        $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}")
);

            var secureHash = HmacSHA512(
                _config["VnPay:HashSecret"],
                hashData
            );

            // ✅ 2. QUERY STRING (CÓ URL ENCODE)
            var queryString = string.Join("&",
    vnpay.Select(kv =>
        $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}")
);

            return $"{_config["VnPay:BaseUrl"]}?{queryString}&vnp_SecureHash={secureHash}";
        }


        public async Task<(bool Success, string Message, int OrderId)> ProcessVnPayReturnAsync(IQueryCollection query)
        {
            int orderId = 0;
            try
            {
                if (query.Count == 0) return (false, "Invalid parameters", 0);

                var vnp_SecureHash = query["vnp_SecureHash"].ToString();
                var vnp_TxnRef = query["vnp_TxnRef"].ToString();
                var vnp_ResponseCode = query["vnp_ResponseCode"].ToString();
                // var vnp_TransactionNo = query["vnp_TransactionNo"].ToString(); // Có thể log lại nếu cần

                // Parse OrderId
                if (vnp_TxnRef.Contains("_"))
                    orderId = int.Parse(vnp_TxnRef.Split('_')[0]);
                else
                    orderId = int.Parse(vnp_TxnRef);

                // Validate Signature
                var signData = string.Join("&", query
    .Where(x => x.Key.StartsWith("vnp_") &&
                x.Key != "vnp_SecureHash" &&
                x.Key != "vnp_SecureHashType")
    .OrderBy(x => x.Key)
    .Select(x =>
        $"{x.Key}={WebUtility.UrlEncode(x.Value)}"));


                var checkSignature = HmacSHA512(_config["VnPay:HashSecret"], signData);

                _logger.LogInformation("RETURN SIGN DATA: {SignData}", signData);
                _logger.LogInformation("RETURN HASH CALC: {Hash}", checkSignature);
                _logger.LogInformation("RETURN HASH FROM VNPAY: {Hash}", vnp_SecureHash);


                if (!checkSignature.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase))
                {
                    return (false, "Lỗi bảo mật: Chữ ký không hợp lệ", orderId);
                }

                // Lấy Order
                var order = await _orderRepository.GetByIdAsync(orderId, includeDetails: true);
                if (order == null) return (false, "Đơn hàng không tồn tại", orderId);

                if (vnp_ResponseCode == "00") // Thành công
                {
                    // Trừ kho
                    //await _inventoryService.ProcessPaymentInventoryAsync(orderId, "Paid");
                    await _inventoryService.DeductInventoryAsync(orderId);

                    // Cập nhật trạng thái Order & Payment
                    order.Status = "Completed"; // Hardcode string
                    if (order.Payment != null)
                    {
                        order.Payment.Status = "Paid"; // Hardcode string
                        order.Payment.PaidAt = DateTime.Now;
                        // Lưu ý: Không lưu TransactionId vào DB vì Entity không có trường này (theo yêu cầu)
                    }

                    await _orderRepository.UpdateAsync(order);
                    return (true, "Thanh toán thành công!", orderId);
                }
                else // Thất bại
                {
                    if (order.Payment != null)
                    {
                        order.Payment.Status = "Failed"; // Hardcode string
                        await _orderRepository.UpdateAsync(order);
                    }
                    return (false, $"Thanh toán thất bại. Mã lỗi: {vnp_ResponseCode}", orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay Return Error");
                return (false, "Lỗi xử lý hệ thống", orderId);
            }
        }

        private static string HmacSHA512(string key, string input)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue) hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
