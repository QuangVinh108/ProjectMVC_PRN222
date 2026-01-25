using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class WishlistProductDTO
    {
        public int WishlistProductId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string? ProductImage { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Sku { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public DateTime AddedAt { get; set; }
        public string? Note { get; set; } = string.Empty;
    }

    public class WishListCountDTO
    {
        public int Count { get; set; }
    }
}
