using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ShopDbContext _context;

        public OrderRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int orderId, bool includeDetails = false)
        {
            var query = _context.Orders.AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Payment)
                    .Include(o => o.Shipping)
                    .Include(o => o.User);
            }

            return await query.FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .Include(o => o.Shipping)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.Shipping)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.OrderId == orderId);
        }
        public async Task<int> GetTotalOrderCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems) // ✅ OrderItems thay vì OrderDetails
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersThisMonthAsync()
        {
            var firstDayThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await _context.Orders
                .Include(o => o.OrderItems) // ✅ Include để tính revenue
                .Where(o => o.OrderDate >= firstDayThisMonth)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersLastMonthAsync()
        {
            var now = DateTime.Now;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            return await _context.Orders
                .Include(o => o.OrderItems) // ✅ Include để tính revenue
                .Where(o => o.OrderDate >= firstDayLastMonth && o.OrderDate < firstDayThisMonth)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetOrdersByStatusAsync()
        {
            return await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status ?? "Unknown", x => x.Count);
        }

        public async Task<List<Order>> GetRecentOrdersAsync(int count = 10)
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();
        }


    }
}

