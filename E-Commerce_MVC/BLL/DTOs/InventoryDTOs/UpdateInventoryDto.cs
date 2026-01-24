using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.InventoryDTOs
{
    public class UpdateInventoryDto
    {
        public int ProductId { get; set; }
        public int? Quantity { get; set; }
        public string? Warehouse { get; set; }
    }
}
