using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs.InventoryDTOs;
using BLL.Helper;
using DAL.Entities;

namespace BLL.IService
{
    public interface IInventoryService
    {
        Task<GenericResult<InventoryDto?>> GetByProductIdAsync(int productId);
        Task<GenericResult<PagedResult<InventoryDto>>> GetAllAsync(QueryInventoryDTO query);
        Task<GenericResult<InventoryDto>> CreateAsync(CreateInventoryDto inventory);
        Task<GenericResult<InventoryDto?>> UpdateAsync(int productId, UpdateInventoryDto inventory);
        Task<GenericResult<bool>> DeleteAsync(int productId);
        Task<GenericResult<bool>> UpdateQuantityAsync(int productId, int quantity);
        Task<GenericResult<bool>> HasStockAsync(int productId, int quantity);
        Task<GenericResult<int>> GetAvailableStockAsync(int productId);
        Task<GenericResult<bool>> ProcessPaymentInventoryAsync(int orderId, string paymentStatus);
    }
}
