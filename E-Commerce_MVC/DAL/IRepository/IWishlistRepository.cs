using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.IRepository
{
    public interface IWishlistRepository
    {
        Task<IEnumerable<Wishlist>> GetAllAsync();
        Task<Wishlist> GetByIdAsync(int id);
        Task AddAsync(Wishlist wishlist);
        Task UpdateAsync(Wishlist wishlist);
        Task DeleteAsync(int id);
        Task<int> GetCountByUserAsync(int userId);
        Task<Wishlist> GetWishlistByUserAsync(int userId);
        Task<WishlistProduct?> GetWishlistProductAsync(int wishlistProductId);
        Task<WishlistProduct> AddWishlistProductAsync(WishlistProduct wishlistProduct);
        Task<bool> RemoveWishlistProductAsync(int wishlistProductId);
        Task<bool> ClearWishlistAsync(int userId);
        Task<IEnumerable<WishlistProduct>> GetAllWishlistProductsAsync();
        Task<bool> IsProductInWishlistAsync(int userId, int productId);
        Task<int> GetWishlistProductIdAsync(int userId, int productId);

    }
}
