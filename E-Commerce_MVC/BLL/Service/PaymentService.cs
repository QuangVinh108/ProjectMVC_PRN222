using BLL.DTOs;
using BLL.IService;
using DAL.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BLL.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IInventoryService inventoryService,
            IPaymentRepository paymentRepository,
            IConfiguration config,
            ILogger<PaymentService> logger)
        {
            _inventoryService = inventoryService;
            _paymentRepository = paymentRepository;
            _config = config;
            _logger = logger;
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
                { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                { "vnp_SecureHashType", "HmacSHA512" }
            };

            // 1️⃣ HashData – PHẢI URL ENCODE
            var hashData = string.Join("&",
                vnpay
                    .Where(kv => kv.Key != "vnp_SecureHashType")
                    .Select(kv =>
                        $"{kv.Key}={Uri.EscapeDataString(kv.Value)}")
            );

            // 2️⃣ QueryString – giữ nguyên
            var queryString = string.Join("&",
                vnpay.Select(kv =>
                    $"{kv.Key}={Uri.EscapeDataString(kv.Value)}")
            );

            var secureHash = HmacSHA512(_config["VnPay:HashSecret"], hashData);
            var paymentUrl = $"{_config["VnPay:BaseUrl"]}?{queryString}&vnp_SecureHash={secureHash}";

            _logger.LogInformation("Create VNPay URL - OrderId: {OrderId}", payment.OrderId);

            return paymentUrl;

        }


        public bool HandleVnPayReturn(IQueryCollection query, out int orderId)
        {
            orderId = 0;
            try
            {
                // 1. PARSE ORDER ID
                var txnRef = query["vnp_TxnRef"].ToString();
                orderId = int.Parse(txnRef);

                // 2. VALIDATE SIGNATURE
                var receivedHash = query["vnp_SecureHash"].ToString();
                var signData = string.Join("&",
                    query.Where(x => x.Key.StartsWith("vnp_") &&
                                   x.Key != "vnp_SecureHash" &&
                                   x.Key != "vnp_SecureHashType")
                        .OrderBy(x => x.Key)
                        .Select(x => $"{x.Key}={x.Value}"));

                var calculatedHash = HmacSHA512(_config["VnPay:HashSecret"], signData);
                var isValidSignature = receivedHash.Equals(calculatedHash, StringComparison.OrdinalIgnoreCase);

                // 3. GET VNPAY STATUS
                var vnpResponseCode = query["vnp_ResponseCode"].ToString();
                var vnpTransactionStatus = query["vnp_TransactionStatus"].ToString();
                var paymentDbStatus = GetPaymentStatus(vnpResponseCode, vnpTransactionStatus);
                var isPaymentSuccess = vnpResponseCode == "00";

                _logger.LogInformation("🔍 VNPay Callback - OrderId: {OrderId}, Sig: {Valid}, Status: {Status}",
                    orderId, isValidSignature, paymentDbStatus);

                // 4. 🔥 CRITICAL: PROCESS INVENTORY + UPDATE DB
                if (isValidSignature)
                {
                    // INVENTORY: Trừ stock nếu Paid, cộng lại nếu Failed
                    var inventoryStatus = paymentDbStatus == "Paid" ? "Paid" : "Failed";
                    var inventoryResult = _inventoryService.ProcessPaymentInventoryAsync(orderId, inventoryStatus).Result;

                    _logger.LogInformation("📦 Inventory Result - OrderId: {OrderId}, Success: {Success}",
                        orderId, inventoryResult.IsSuccess);

                    // UPDATE PAYMENT DB
                    DateTime? paidAt = isPaymentSuccess ? DateTime.UtcNow : null;
                    var paymentRows = _paymentRepository.UpdateStatusAsync(orderId, paymentDbStatus, paidAt).Result;

                    var allSuccess = inventoryResult.IsSuccess && paymentRows > 0;

                    _logger.LogInformation("🎯 SYNC COMPLETE - OrderId: {OrderId}, Inv: {InvOk}, Pay: {PayRows} ({AllOk})",
                    orderId, inventoryResult.IsSuccess, paymentRows, allSuccess);
                }

                return isValidSignature && isPaymentSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 VNPay Return Error");
                return false;
            }
        }

        private static string GetPaymentStatus(string vnpResponseCode, string vnpTransactionStatus)
        {
            return (vnpResponseCode, vnpTransactionStatus) switch
            {
                ("00", "00") => "Paid",
                ("00", _) => "Pending",
                ("07", _) => "Failed",
                ("09", _) => "Failed",
                ("99", _) => "Failed",
                ("24", _) => "Cancelled",
                _ => "Failed"
            };
        }




        private static string HmacSHA512(string key, string input)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)))
                .Replace("-", "")
                .ToUpper();
        }
    }
}
