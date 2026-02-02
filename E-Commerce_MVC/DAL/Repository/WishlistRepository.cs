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
    public class WishlistRepository : IWishlistRepository
    {
        private readonly ShopDbContext _context;
        public WishlistRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Wishlist>> GetAllAsync()
        {
            return await _context.Wishlists.ToListAsync();
        }

        public async Task<Wishlist> GetByIdAsync(int id)
        {
            return await _context.Wishlists.FindAsync(id);
        }

        public async Task AddAsync(Wishlist wishlist)
        {
            await _context.Wishlists.AddAsync(wishlist);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Wishlist wishlist)
        {
            _context.Wishlists.Update(wishlist);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var wishlist = await _context.Wishlists.FindAsync(id);
            if (wishlist != null)
            {
                _context.Wishlists.Remove(wishlist);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCountByUserAsync(int userId)
        {
            return await _context.WishlistProducts
                .Where(wp => wp.Wishlist.UserId == userId)
                .CountAsync();
        }

        public async Task<Wishlist?> GetWishlistByUserAsync(int userId)
        {
            return await _context.Wishlists
                .Include(w => w.WishlistProducts)!
                    .ThenInclude(wp => wp.Product)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<WishlistProduct?> GetWishlistProductAsync(int wishlistProductId)
        {
            return await _context.WishlistProducts
                .Include(wp => wp.Product)
                .FirstOrDefaultAsync(wp => wp.WishlistProductId == wishlistProductId);
        }

        public async Task<WishlistProduct> AddWishlistProductAsync(WishlistProduct wishlistProduct)
        {
            _context.WishlistProducts.Add(wishlistProduct);
            await _context.SaveChangesAsync();
            return wishlistProduct;
        }

        public async Task<bool> RemoveWishlistProductAsync(int wishlistProductId)
        {
            var product = await GetWishlistProductAsync(wishlistProductId);
            if (product == null) return false;

            _context.WishlistProducts.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearWishlistAsync(int userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistProducts)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist?.WishlistProducts != null && wishlist.WishlistProducts.Any())
            {
                _context.WishlistProducts.RemoveRange(wishlist.WishlistProducts);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<WishlistProduct>> GetAllWishlistProductsAsync()
        {
            return await _context.WishlistProducts
                .Include(wp => wp.Wishlist)
                .Include(wp => wp.Product)
                .ToListAsync();
        }

        public async Task<bool> IsProductInWishlistAsync(int userId, int productId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistProducts)
                .ThenInclude(wp => wp.Product)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            return wishlist?.WishlistProducts?.Any(wp => wp.ProductId == productId) == true;
        }


        public async Task<int> GetWishlistProductIdAsync(int userId, int productId)
        {
            return await _context.WishlistProducts
                .Where(wp => wp.Wishlist.UserId == userId && wp.ProductId == productId)
                .Select(wp => wp.WishlistProductId)
                .FirstOrDefaultAsync();
        }

    }
}
