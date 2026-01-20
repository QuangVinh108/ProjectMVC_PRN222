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
    }
}
