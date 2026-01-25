using BLL.DTOs;
using BLL.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BLL.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IConfiguration _config;

        public PaymentService(IConfiguration config)
        {
            _config = config;
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

    // 🔥 SỬA NGAY DÒNG NÀY
    { "vnp_TxnRef", $"{payment.OrderId}_{DateTime.Now:HHmmss}" },

    { "vnp_OrderInfo", $"Thanh_toan_don_hang_{payment.OrderId}" },
    { "vnp_OrderType", "other" },
    { "vnp_Locale", "vn" },
    { "vnp_ReturnUrl", _config["VnPay:ReturnUrl"] },
    { "vnp_IpAddr", "127.0.0.1" },
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


            // 🔥 DEBUG – BẮT BUỘC
            Console.WriteLine("===== VNPAY HASH DATA =====");
            Console.WriteLine(hashData);

            Console.WriteLine("===== VNPAY URL =====");
            Console.WriteLine($"{_config["VnPay:BaseUrl"]}?{queryString}&vnp_SecureHash={secureHash}");

            return $"{_config["VnPay:BaseUrl"]}?{queryString}&vnp_SecureHash={secureHash}";
        }


        public bool HandleVnPayReturn(IQueryCollection query, out int orderId)
        {
            var txnRef = query["vnp_TxnRef"].ToString();
            orderId = int.Parse(txnRef.Split('_')[0]);

            var receivedHash = query["vnp_SecureHash"].ToString();

            var data = query
                .Where(x =>
                    x.Key.StartsWith("vnp_") &&
                    x.Key != "vnp_SecureHash" &&
                    x.Key != "vnp_SecureHashType"
                )
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}");

            var calculatedHash = HmacSHA512(
                _config["VnPay:HashSecret"],
                string.Join("&", data)
            );

            // 🔥 LOG DEBUG
            Console.WriteLine("VNP_ResponseCode = " + query["vnp_ResponseCode"]);
            Console.WriteLine("VNP_TransactionStatus = " + query["vnp_TransactionStatus"]);
            Console.WriteLine("VNP_ReceivedHash = " + receivedHash);
            Console.WriteLine("VNP_CalculatedHash = " + calculatedHash);

            return receivedHash.Equals(calculatedHash, StringComparison.OrdinalIgnoreCase)
                && query["vnp_ResponseCode"] == "00"
                && query["vnp_TransactionStatus"] == "00";
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
