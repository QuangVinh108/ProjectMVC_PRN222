using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
using BLL.Helper;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BLL.Service
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IProductRepository _productRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public WishlistService(
            IWishlistRepository wishlistRepo,
            IProductRepository productRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _wishlistRepository = wishlistRepo;
            _productRepo = productRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return 0;

            // ⭐ DEBUG: Log tất cả claims
            Console.WriteLine("🔍 All Claims:");
            foreach (var claim in httpContext.User.Claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }

            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value ??
                             httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                             httpContext.User.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        public async Task<bool> IsProductInWishlistAsync(int productId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return false;

                return await _wishlistRepository.IsProductInWishlistAsync(userId, productId);
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<GenericResult<IEnumerable<WishlistProductDTO>>> GetUserWishlistAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return GenericResult<IEnumerable<WishlistProductDTO>>.Failure("User not authenticated");

                var wishlist = await _wishlistRepository.GetWishlistByUserAsync(userId);
                if (wishlist?.WishlistProducts == null)
                    return GenericResult<IEnumerable<WishlistProductDTO>>.Success([]);

                var dtoItems = wishlist.WishlistProducts.Select(wp => new WishlistProductDTO
                {
                    WishlistProductId = wp.WishlistProductId,
                    ProductId = wp.ProductId,
                    ProductName = wp.Product.ProductName,
                    ProductImage = wp.Product.Image,
                    Price = wp.Product.Price,
                    Sku = wp.Product.Sku,
                    AddedAt = wp.AddedAt,
                    Note = wp.Note
                });

                return GenericResult<IEnumerable<WishlistProductDTO>>.Success(dtoItems);
            }
            catch (Exception ex)
            {
                return GenericResult<IEnumerable<WishlistProductDTO>>.Failure($"Error loading wishlist: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> AddToWishlistAsync(int productId, string? note = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return GenericResult<bool>.Failure("User not authenticated");

                var product = _productRepo.GetProductById(productId);
                if (product == null)
                    return GenericResult<bool>.Failure("Product not found");

                var wishlist = await _wishlistRepository.GetWishlistByUserAsync(userId);
                if (wishlist == null)
                {
                    wishlist = new Wishlist { UserId = userId, CreatedAt = DateTime.UtcNow };
                    await _wishlistRepository.AddAsync(wishlist);
                }

                // Check duplicate
                var existing = wishlist.WishlistProducts.FirstOrDefault(wp => wp.ProductId == productId);
                if (existing != null)
                    return GenericResult<bool>.Success(false, "Product already in wishlist");

                var wishlistProduct = new WishlistProduct
                {
                    WishlistId = wishlist.WishlistId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow,
                    Note = note,
                    Image = product.Image
                };

                await _wishlistRepository.AddWishlistProductAsync(wishlistProduct);
                return GenericResult<bool>.Success(true, "Added to wishlist");
            }
            catch (Exception ex)
            {
                return GenericResult<bool>.Failure($"Error adding to wishlist: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> RemoveFromWishlistAsync(int wishlistProductId)
        {
            try
            {
                var result = await _wishlistRepository.RemoveWishlistProductAsync(wishlistProductId);
                return GenericResult<bool>.Success(result, result ? "Removed from wishlist" : "Not found");
            }
            catch (Exception ex)
            {
                return GenericResult<bool>.Failure($"Error removing from wishlist: {ex.Message}");
            }
        }

        public async Task<GenericResult<int>> GetWishlistCountAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return GenericResult<int>.Success(0);

                var count = await _wishlistRepository.GetCountByUserAsync(userId);
                return GenericResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return GenericResult<int>.Failure($"Error getting count: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> ClearWishlistAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return GenericResult<bool>.Failure("User not authenticated");

                var result = await _wishlistRepository.ClearWishlistAsync(userId);
                return GenericResult<bool>.Success(
                    result,
                    result ? "Wishlist cleared successfully" : "No wishlist items found"
                );
            }
            catch (Exception ex)
            {
                return GenericResult<bool>.Failure($"Error clearing wishlist: {ex.Message}");
            }
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

        public async Task<GenericResult<IEnumerable<WishlistProduct>>> GetAllWishlistProductsForAdminAsync()
        {
            try
            {
                var products = await _wishlistRepository.GetAllWishlistProductsAsync();
                return GenericResult<IEnumerable<WishlistProduct>>.Success(products);
            }
            catch (Exception ex)
            {
                return GenericResult<IEnumerable<WishlistProduct>>.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<GenericResult<bool>> ToggleWishlistAsync(int productId)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return GenericResult<bool>.Failure("Chưa đăng nhập");

            var isInWishlist = await _wishlistRepository.IsProductInWishlistAsync(userId, productId);

            if (isInWishlist)
            {
                // REMOVE
                var wishlistProductId = await _wishlistRepository.GetWishlistProductIdAsync(userId, productId);
                if (wishlistProductId > 0)
                {
                    await _wishlistRepository.RemoveWishlistProductAsync(wishlistProductId);
                    return GenericResult<bool>.Success(false, "Đã xóa khỏi wishlist");
                }
                return GenericResult<bool>.Failure("Không tìm thấy trong wishlist");
            }
            else
            {
                // ADD
                var wishlist = await _wishlistRepository.GetWishlistByUserAsync(userId);
                if (wishlist == null)
                {
                    wishlist = new Wishlist { UserId = userId, CreatedAt = DateTime.UtcNow };
                    await _wishlistRepository.AddAsync(wishlist);
                }

                var product = _productRepo.GetProductById(productId);
                var wishlistProduct = new WishlistProduct
                {
                    WishlistId = wishlist.WishlistId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow,
                    Image = product?.Image
                };
                await _wishlistRepository.AddWishlistProductAsync(wishlistProduct);
                return GenericResult<bool>.Success(true, "Đã thêm vào wishlist");
            }
        }

        public async Task<GenericResult<bool>> CreateEmptyWishlistForUserAsync(int userId, string? note = null)
        {
            try
            {
                if (userId <= 0)
                    return GenericResult<bool>.Failure("Invalid user ID");

                var wishlist = await _wishlistRepository.GetWishlistByUserAsync(userId);
                if (wishlist != null)
                    return GenericResult<bool>.Success(false, "Wishlist already exists");

                wishlist = new Wishlist
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _wishlistRepository.AddAsync(wishlist);

                Console.WriteLine($"✅ Created empty wishlist for user {userId}");
                return GenericResult<bool>.Success(true, "Wishlist created successfully");
            }
            catch (Exception ex)
            {
                return GenericResult<bool>.Failure($"Error creating wishlist: {ex.Message}");
            }
        }

    }
}
