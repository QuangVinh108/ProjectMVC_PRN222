using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Repositories.IRepository;

namespace Repositories.Repository
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
            return await _context.Wishlists.CountAsync(w => w.UserId == userId);
        }

        public async Task<Wishlist> GetWishlistByUserAsync(int userId)
        {
            return await _context.Wishlists
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.UserId == userId)
                ;
        }
    }
}
