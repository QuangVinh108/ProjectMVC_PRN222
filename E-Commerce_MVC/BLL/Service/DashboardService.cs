using BLL.DTOs;
using BLL.IService;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IUserRepository _userRepo;
        private readonly IProductRepository _productRepo;

        public DashboardService(
            IOrderRepository orderRepo,
            IUserRepository userRepo,
            IProductRepository productRepo)
        {
            _orderRepo = orderRepo;
            _userRepo = userRepo;
            _productRepo = productRepo;
        }

        public async Task<DashboardStatisticsDTO> GetDashboardStatisticsAsync()
        {
            var totalUsers = await _userRepo.GetTotalUserCountAsync();
            var totalProducts = await _productRepo.GetTotalProductCountAsync();
            var totalOrders = await _orderRepo.GetTotalOrderCountAsync();
            var totalRevenue = await _orderRepo.GetTotalRevenueAsync();

            // Tháng này
            var ordersThisMonth = await _orderRepo.GetOrdersThisMonthAsync();
            var revenueThisMonth = ordersThisMonth
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => o.TotalAmount);
            var ordersCountThisMonth = ordersThisMonth.Count;
            var newUsersThisMonth = await _userRepo.GetNewUsersCountThisMonthAsync();

            // Tháng trước
            var ordersLastMonth = await _orderRepo.GetOrdersLastMonthAsync();
            var revenueLastMonth = ordersLastMonth
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => o.TotalAmount);
            var ordersCountLastMonth = ordersLastMonth.Count;
            var newUsersLastMonth = await _userRepo.GetNewUsersCountLastMonthAsync();

            // Tính % tăng trưởng
            var revenueGrowth = revenueLastMonth > 0
                ? ((double)(revenueThisMonth - revenueLastMonth) / (double)revenueLastMonth) * 100
                : 0;

            var orderGrowth = ordersCountLastMonth > 0
                ? ((double)(ordersCountThisMonth - ordersCountLastMonth) / ordersCountLastMonth) * 100
                : 0;

            var userGrowth = newUsersLastMonth > 0
                ? ((double)(newUsersThisMonth - newUsersLastMonth) / newUsersLastMonth) * 100
                : 0;

            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return new DashboardStatisticsDTO
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                OrdersThisMonth = ordersCountThisMonth,
                NewUsersThisMonth = newUsersThisMonth,
                AverageOrderValue = avgOrderValue,
                RevenueGrowthPercent = Math.Round(revenueGrowth, 2),
                OrderGrowthPercent = Math.Round(orderGrowth, 2),
                UserGrowthPercent = Math.Round(userGrowth, 2)
            };
        }

        public async Task<RevenueChartDTO> GetRevenueChartDataAsync(int days = 30)
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-days);

            var orders = await _orderRepo.GetOrdersByDateRangeAsync(startDate, endDate);
            var completedOrders = orders.Where(o => o.Status == "Hoàn thành").ToList();

            var revenueByDay = completedOrders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToList();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                labels.Add(date.ToString("dd/MM"));

                var dayRevenue = revenueByDay.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0;
                data.Add(dayRevenue);
            }

            return new RevenueChartDTO { Labels = labels, Data = data };
        }

        public async Task<List<TopProductDTO>> GetTopProductsAsync(int top = 5)
        {
            var allOrders = await _orderRepo.GetAllAsync();

            // ✅ Sử dụng UnitPrice từ OrderItem (theo file:96)
            var topProducts = allOrders
                .Where(o => o.IsActive) // Chỉ lấy đơn hàng active
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
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice) // ✅ UnitPrice
                })
                .OrderByDescending(p => p.TotalSold)
                .Take(top)
                .ToList();

            return topProducts;
        }

        public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(int count = 10)
        {
            var recentOrders = await _orderRepo.GetRecentOrdersAsync(count);

            return recentOrders.Select(o => new RecentOrderDTO
            {
                OrderId = o.OrderId,
                CustomerName = o.User?.FullName ?? o.User?.UserName ?? "N/A",
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status
            }).ToList();
        }

        public async Task<OrderStatusChartDTO> GetOrderStatusChartAsync()
        {
            var ordersByStatus = await _orderRepo.GetOrdersByStatusAsync();

            return new OrderStatusChartDTO
            {
                Labels = ordersByStatus.Keys.ToList(),
                Data = ordersByStatus.Values.ToList()
            };
        }

        public async Task<UserGrowthChartDTO> GetUserGrowthChartAsync(int months = 6)
        {
            var userGrowth = await _userRepo.GetUserGrowthByMonthAsync(months);

            var labels = new List<string>();
            var data = new List<int>();

            var now = DateTime.Now;
            for (int i = months - 1; i >= 0; i--)
            {
                var month = now.AddMonths(-i);
                var monthKey = month.Year * 100 + month.Month;

                labels.Add($"{month.Month}/{month.Year}");
                data.Add(userGrowth.ContainsKey(monthKey) ? userGrowth[monthKey] : 0);
            }

            return new UserGrowthChartDTO { Labels = labels, Data = data };
        }
        public async Task<List<ReportResultDTO>> GetReportDataAsync(DateTime startDate, DateTime endDate, string reportType)
        {
            // 1. Chuẩn hóa thời gian: Lấy đến 23:59:59.999 của ngày kết thúc
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            // 2. Điều hướng dựa trên loại báo cáo
            switch (reportType.ToLower())
            {
                case "revenue":
                    return await _orderRepo.GetRevenueReportAsync(startDate, adjustedEndDate);

                case "products":
                    // Mặc định lấy Top 10 sản phẩm
                    return await _orderRepo.GetTopSellingProductsAsync(startDate, adjustedEndDate, 10);

                case "categories":
                    return await _orderRepo.GetCategoryRevenueReportAsync(startDate, adjustedEndDate);

                case "payment_methods":
                    return await _orderRepo.GetRevenueByPaymentMethodAsync(startDate, adjustedEndDate);

                default:
                    // Trả về list rỗng nếu không khớp loại nào
                    return new List<ReportResultDTO>();
            }
        }
    }
}
