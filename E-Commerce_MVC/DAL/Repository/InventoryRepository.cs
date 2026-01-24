using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly ShopDbContext _context;
        public InventoryRepository(ShopDbContext context)
        {
            _context = context;
        }
        public async Task<Inventory?> GetByProductIdAsync(int productId)
        {
            return await _context.Inventories
                                 .Include(i => i.Product)
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(i => i.ProductId == productId);
        }

        public async Task<bool> HasStockAsync(int productId, int quantity)
        {
            var inventory = await GetByProductIdAsync(productId);
            return inventory != null && inventory.Quantity >= quantity;
        }

        public async Task<bool> UpdateQuantityAsync(int productId, int quantity)
        {
            var inventory = await GetByProductIdAsync(productId);
            if(inventory == null)
                return false;

            inventory.Quantity = quantity;
            inventory.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public IQueryable<Inventory> GetAllQueryable(string? includeProperties = null)
        {
            IQueryable<Inventory> query = _context.Inventories.AsNoTracking();

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach(var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return query;
        }

        public async Task<Inventory> CreateAsync(Inventory inventory)
        {
            await _context.Inventories.AddAsync(inventory);
            await _context.SaveChangesAsync();
            await _context.Entry(inventory).ReloadAsync();
            return inventory;
        }

        public async Task<Inventory> UpdateAsync(Inventory inventory)
        {
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
            return inventory;
        }

        public async Task<bool> DeleteAsync(int productId)
        {
            var inventory = await GetByProductIdAsync(productId);
            if (inventory == null)
                return false;

            _context.Inventories.Remove(inventory);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
