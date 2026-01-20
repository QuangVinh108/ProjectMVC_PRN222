using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;

namespace BLL.IService
{
    public interface IWishlistService
    {
        Task<IEnumerable<Wishlist>> GetAllWishListsAsync();
        Task<Wishlist> GetWishlistByIdAsync(int id);
        Task CreateWishlistAsync(Wishlist wishlist);
        Task UpdateWishlistAsync(Wishlist wishlist);
        Task DeleteWishlistAsync(int id);
        Task<int> GetCountByUserAsync(int userId);
        Task<Wishlist> GetWishlistByUserAsync(int userId);
    }
}
