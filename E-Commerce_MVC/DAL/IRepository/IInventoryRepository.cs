using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.IRepository
{
    public interface IInventoryRepository
    {
        Task<Inventory?> GetByProductIdAsync(int productId);
        Task<bool> UpdateQuantityAsync(int productId, int quantity);
        Task<bool> HasStockAsync(int productId, int quantity);
        IQueryable<Inventory> GetAllQueryable(string? includeProperties = null);
        Task<Inventory> CreateAsync(Inventory inventory);
        Task<Inventory?> UpdateAsync(Inventory inventory);
        Task<bool> DeleteAsync(int productId);
    }
}
