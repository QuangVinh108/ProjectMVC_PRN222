using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;
using Services.IService;

namespace Services.Service
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        public WishlistService(IWishlistRepository wishlistRepository)
        {
            _wishlistRepository = wishlistRepository;
        }

        public async Task<Wishlist> GetWishlistByUserAsync(int userId)
        {
            return await _wishlistRepository.GetWishlistByUserAsync(userId);
        }

        public async Task CreateWishlistAsync(Wishlist wishlist)
        {
            await _wishlistRepository.AddAsync(wishlist);
        }

        public async Task DeleteWishlistAsync(int id)
        {
            await _wishlistRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Wishlist>> GetAllWishListsAsync()
        {
            return await _wishlistRepository.GetAllAsync();
        }

        public async Task<Wishlist> GetWishlistByIdAsync(int id)
        {
            return await _wishlistRepository.GetByIdAsync(id);
        }

        public async Task<int> GetCountByUserAsync(int userId)
        {
            return await _wishlistRepository.GetCountByUserAsync(userId);
        }

        public async Task UpdateWishlistAsync(Wishlist wishlist)
        {
            await _wishlistRepository.UpdateAsync(wishlist);
        }
    }
}
