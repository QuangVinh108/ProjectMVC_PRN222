using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BLL.Helpers
{
    public static class StringHelper
    {
        // Hàm chuyển tiếng Việt có dấu thành không dấu, về chữ thường để so sánh
        // VD: "Nguyễn Văn A" -> "nguyen van a"
        public static string NormalizeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            text = text.ToLower().Trim();

            // Bỏ dấu tiếng Việt
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = text.Normalize(NormalizationForm.FormD);
            string result = regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'd');

            // Bỏ khoảng trắng thừa
            return Regex.Replace(result, @"\s+", " ");
        }

        // Hàm so sánh 2 ngày tháng (chấp nhận lệch 1 chút format nếu cần)
        public static bool CompareDates(DateTime date1, string dateStr2)
        {
            if (DateTime.TryParseExact(dateStr2, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return date1.Date == parsedDate.Date;
            }
            return false;
        }
    }
}
