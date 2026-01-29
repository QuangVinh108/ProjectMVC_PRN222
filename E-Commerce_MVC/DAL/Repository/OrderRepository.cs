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
                .Where(o => o.Status == "Hoàn thành")
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

        public async Task<List<ReportResultDTO>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            // Bước 1: Query dữ liệu thô từ SQL (Chưa format string ở đây)
            var rawData = await _context.Orders
                .Where(o => o.OrderDate >= startDate &&
                            o.OrderDate <= endDate &&
                            o.IsActive &&
                            // LƯU Ý: Phải có ngoặc bao quanh các điều kiện OR
                            (o.Status == "Paid" || o.Status == "Completed" || o.Status == "Shipped"))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Value = g.Sum(x => x.TotalAmount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date) // Sắp xếp theo ngày tăng dần ngay trong SQL
                .ToListAsync(); // Thực thi câu lệnh SQL lấy dữ liệu về RAM

            // Bước 2: Format dữ liệu trên RAM (Client evaluation)
            var result = rawData.Select(x => new ReportResultDTO
            {
                Label = x.Date.ToString("dd/MM/yyyy"), // Format ngày tháng ở đây an toàn
                Value = x.Value,
                Count = x.Count
            }).ToList();

            return result;
        }
        public async Task<List<ReportResultDTO>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int top = 5)
        {
            // Query: Join OrderItems -> Products -> Group By Product
            // Cách này tối ưu hơn việc lấy All Order rồi tính toán ở C#
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate &&
                             oi.Order.IsActive &&
                             (oi.Order.Status == "Paid" || oi.Order.Status == "Completed" || oi.Order.Status == "Shipped"))
                .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.Image })
                .Select(g => new ReportResultDTO
                {
                    Label = g.Key.ProductName,
                    ExtraInfo = g.Key.Image, // Lấy ảnh để hiển thị nếu cần
                    Count = g.Sum(x => x.Quantity), // Tổng số lượng bán
                    Value = g.Sum(x => x.Quantity * x.UnitPrice) // Tổng doanh thu từ SP này
                })
                .OrderByDescending(x => x.Count) // Sắp xếp theo số lượng bán giảm dần
                .Take(top);

            return await query.ToListAsync();
        }

        public async Task<List<ReportResultDTO>> GetCategoryRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            // Query: OrderItems -> Product -> Category -> Group By CategoryName
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.OrderDate >= startDate &&
                             oi.Order.OrderDate <= endDate &&
                             oi.Order.IsActive &&
                             (oi.Order.Status == "Paid" || oi.Order.Status == "Completed"))
                .GroupBy(oi => oi.Product.Category.CategoryName)
                .Select(g => new ReportResultDTO
                {
                    Label = g.Key, // Tên danh mục
                    Count = g.Sum(x => x.Quantity), // Tổng sản phẩm thuộc danh mục này đã bán
                    Value = g.Sum(x => x.Quantity * x.UnitPrice) // Tổng doanh thu của danh mục
                });

            return await query.OrderByDescending(x => x.Value).ToListAsync();
        }

        public async Task<List<ReportResultDTO>> GetRevenueByPaymentMethodAsync(DateTime startDate, DateTime endDate)
        {
            // Query: Join bảng Payments
            var query = _context.Orders
                .Include(o => o.Payment)
                .Where(o => o.OrderDate >= startDate &&
                            o.OrderDate <= endDate &&
                            o.IsActive &&
                            o.Payment != null) // Chỉ lấy đơn có thông tin thanh toán
                .GroupBy(o => o.Payment.PaymentMethod)
                .Select(g => new ReportResultDTO
                {
                    Label = g.Key, // COD, CreditCard, Momo...
                    Count = g.Count(),
                    Value = g.Sum(x => x.TotalAmount)
                });

            return await query.ToListAsync();
        }
        public async Task<decimal> GetCompletedRevenueThisMonthAsync()
        {
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

            return await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth
                         && o.OrderDate < now
                         && o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<decimal> GetCompletedRevenueLastMonthAsync()
        {
            var now = DateTime.Now;
            var firstDayOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var firstDayOfThisMonth = new DateTime(now.Year, now.Month, 1);

            return await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfLastMonth
                         && o.OrderDate < firstDayOfThisMonth
                         && o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.OrderDate >= startDate
                         && o.OrderDate <= endDate
                         && o.Status == "Hoàn thành")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(x => x.Date, x => x.Revenue);
        }

        public async Task<List<TopProductDTO>> GetTopSellingProductsAsync(int top)
        {
            return await _context.Orders
                .Where(o => o.IsActive)
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.Product.ProductName,
                    oi.Product.Image
                })
                .Select(g => new TopProductDTO
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Image = g.Key.Image,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(top)
                .ToListAsync();
        }

    }
}

