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
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
            if(inventory == null || quantity < 0)
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

        public async Task<(IList<Inventory> Items, int TotalCount)> GetPagedAsync(string? search, string sortBy, bool isDescending, int pageIndex, int pageSize )
        {
            IQueryable<Inventory> query = _context.Inventories
                                                    .Include(i => i.Product)
                                                    .AsNoTracking();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var like = $"%{search}%";
                query = query.Where(i => EF.Functions.Like(i.Product.ProductName, like) ||
                                                 EF.Functions.Like(i.Warehouse, like));
            }

            // Sort
            query = sortBy switch
            {
                "quantity" => isDescending
                ? query.OrderByDescending(i => i.Quantity)
                : query.OrderBy(i => i.Quantity),
                "warehouse" => isDescending
                ? query.OrderByDescending(i => i.Warehouse)
                : query.OrderBy(i => i.Warehouse),
                "updatedat" => isDescending
                ? query.OrderByDescending(i => i.UpdatedAt)
                : query.OrderBy(i => i.UpdatedAt),
                _ => isDescending
                ? query.OrderByDescending(i => i.Product.ProductName)
                : query.OrderBy(i => i.Product.ProductName),
            };

            // Total Count
            var totalCount = await query.CountAsync();

            // Paging
            var items = await query.Skip((pageIndex - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();
            return (items, totalCount);
        }

        public async Task<int> GetQuantityAsync(int productId)
        {
            var inventory = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.ProductId == productId)
                .Select(i => i.Quantity)
                .FirstOrDefaultAsync();
            return inventory;
        }
    }
}
