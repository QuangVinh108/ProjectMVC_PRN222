using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly ShopDbContext _context;

        public CartRepository(ShopDbContext context)
        {
            _context = context;
        }

        public Cart GetCartByUserId(int userId)
        {
            var cart = _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            return cart;
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task ClearCartAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart != null && cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
            }
        }

        public void AddItem(int userId, int productId, int quantity)
        {
            var cart = GetCartByUserId(userId);

            var item = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == productId);

            if (item == null)
            {
                var product = _context.Products.Find(productId);

                if (product == null) return;

                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };

                _context.CartItems.Add(cartItem);
            }
            else
            {
                item.Quantity += quantity;
            }

            cart.UpdatedAt = DateTime.Now;
            _context.SaveChanges();
        }

        public void UpdateQuantity(int cartItemId, int quantity)
        {
            var item = _context.CartItems.Find(cartItemId);
            if (item == null) return;

            item.Quantity = quantity;
            _context.SaveChanges();
        }

        public void RemoveItem(int cartItemId)
        {
            var item = _context.CartItems.Find(cartItemId);
            if (item == null) return;

            _context.CartItems.Remove(item);
            _context.SaveChanges();
        }

        public async Task AddOrReplaceSingleItemAsync(int userId, int productId, int quantity)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // ❗ XÓA HẾT ITEM CŨ → đảm bảo cart chỉ có 1 sản phẩm
            if (cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new Exception("Sản phẩm không tồn tại");

            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price
            };

            _context.CartItems.Add(cartItem);
            cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

    }
}
