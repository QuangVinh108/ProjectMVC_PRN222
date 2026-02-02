using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.InventoryDTOs
{
    public class CreateInventoryDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be >= 0")]
        public int Quantity { get; set; } = 0;
        public string? Warehouse { get; set; }
    }
}
