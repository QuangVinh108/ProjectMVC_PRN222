using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class GeminiIdCardDto
    {
        public bool IsValid { get; set; } // Ảnh có hợp lệ không
        public string Reason { get; set; } // Lý do nếu không hợp lệ
        public IdCardData Data { get; set; } // Dữ liệu bóc tách
    }
    public class IdCardData
    {
        public string IdNumber { get; set; }
        public string FullName { get; set; }
        public string Dob { get; set; } // Để string cho dễ parse sau
        public string Address { get; set; }
    }
}
