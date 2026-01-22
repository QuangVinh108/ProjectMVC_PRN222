using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string? Sku { get; set; } // Mã SKU
        public decimal Price { get; set; }
        public string CategoryName { get; set; } // Hiển thị tên danh mục
        public byte Status { get; set; } // 1: Active, 0: Inactive
        public string? Description { get; set; }
        public string? Image { get; set; } // Thêm dòng này để hiển thị ảnh
    }
}
