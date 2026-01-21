using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Helpers
{
    public class OtpHelper
    {
        // ✅ Lưu OTP trong memory (Dictionary)
        private static Dictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();

        /// <summary>
        /// Tạo mã OTP ngẫu nhiên 6 số
        /// </summary>
        public static string GenerateOtp()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Lưu OTP với thời hạn (mặc định 5 phút)
        /// </summary>
        public static void StoreOtp(string email, string otp, int expiryMinutes = 5)
        {
            _otpStorage[email] = (otp, DateTime.UtcNow.AddMinutes(expiryMinutes));

            // Cleanup expired OTPs
            CleanupExpiredOtps();

            Console.WriteLine($"📝 OTP stored for {email}: {otp} (expires in {expiryMinutes} min)");
        }

        /// <summary>
        /// Validate OTP - Trả về true nếu đúng và còn hiệu lực
        /// </summary>
        public static bool ValidateOtp(string email, string otp)
        {
            if (!_otpStorage.ContainsKey(email))
            {
                Console.WriteLine($"❌ No OTP found for {email}");
                return false;
            }

            var (storedOtp, expiry) = _otpStorage[email];

            // Kiểm tra hết hạn
            if (DateTime.UtcNow > expiry)
            {
                _otpStorage.Remove(email);
                Console.WriteLine($"⏱️ OTP expired for {email}");
                return false;
            }

            // Kiểm tra OTP đúng
            if (otp != storedOtp)
            {
                Console.WriteLine($"❌ Wrong OTP for {email}. Expected: {storedOtp}, Got: {otp}");
                return false;
            }

            // ✅ Valid OTP - remove after use (one-time use)
            _otpStorage.Remove(email);
            Console.WriteLine($"✅ OTP validated and removed for {email}");
            return true;
        }

        /// <summary>
        /// Kiểm tra OTP có hết hạn chưa
        /// </summary>
        public static bool IsOtpExpired(string email)
        {
            if (!_otpStorage.ContainsKey(email))
                return true;

            var (_, expiry) = _otpStorage[email];
            return DateTime.UtcNow > expiry;
        }

        /// <summary>
        /// Xóa OTP của email cụ thể
        /// </summary>
        public static void RemoveOtp(string email)
        {
            if (_otpStorage.Remove(email))
            {
                Console.WriteLine($"🗑️ OTP removed for {email}");
            }
        }

        /// <summary>
        /// Lấy thời gian còn lại của OTP (giây)
        /// </summary>
        public static int GetRemainingSeconds(string email)
        {
            if (!_otpStorage.ContainsKey(email))
                return 0;

            var (_, expiry) = _otpStorage[email];
            var remaining = (expiry - DateTime.UtcNow).TotalSeconds;
            return remaining > 0 ? (int)remaining : 0;
        }

        /// <summary>
        /// Dọn dẹp các OTP đã hết hạn
        /// </summary>
        private static void CleanupExpiredOtps()
        {
            var expiredEmails = _otpStorage
                .Where(x => DateTime.UtcNow > x.Value.Expiry)
                .Select(x => x.Key)
                .ToList();

            foreach (var email in expiredEmails)
            {
                _otpStorage.Remove(email);
                Console.WriteLine($"🧹 Cleaned up expired OTP for {email}");
            }
        }

        /// <summary>
        /// Debug: Xem tất cả OTP đang lưu
        /// </summary>
        public static void DebugPrintAllOtps()
        {
            Console.WriteLine("=== OTP STORAGE DEBUG ===");
            if (_otpStorage.Count == 0)
            {
                Console.WriteLine("(Empty)");
                return;
            }

            foreach (var kvp in _otpStorage)
            {
                var timeLeft = (kvp.Value.Expiry - DateTime.UtcNow).TotalSeconds;
                Console.WriteLine($"📧 {kvp.Key}: {kvp.Value.Otp} (expires in {timeLeft:F0}s)");
            }
        }
    }
}
