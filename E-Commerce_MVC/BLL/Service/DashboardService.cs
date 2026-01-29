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

            // ✅ Gọi trực tiếp từ Repository
            var revenueThisMonth = await _orderRepo.GetCompletedRevenueThisMonthAsync();
            var revenueLastMonth = await _orderRepo.GetCompletedRevenueLastMonthAsync();

            var ordersThisMonth = await _orderRepo.GetOrdersThisMonthAsync();
            var ordersLastMonth = await _orderRepo.GetOrdersLastMonthAsync();
            var ordersCountThisMonth = ordersThisMonth.Count;
            var ordersCountLastMonth = ordersLastMonth.Count;

            var newUsersThisMonth = await _userRepo.GetNewUsersCountThisMonthAsync();
            var newUsersLastMonth = await _userRepo.GetNewUsersCountLastMonthAsync();

            // ✅ Service chỉ tính toán business logic
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

            // ✅ Gọi Repository để lấy data
            var revenueByDay = await _orderRepo.GetDailyRevenueAsync(startDate, endDate);

            var labels = new List<string>();
            var data = new List<decimal>();

            // ✅ Service chỉ format dữ liệu
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                labels.Add(date.ToString("dd/MM"));

                var dayRevenue = revenueByDay.ContainsKey(date) ? revenueByDay[date] : 0;
                data.Add(dayRevenue);
            }

            return new RevenueChartDTO { Labels = labels, Data = data };
        }

        public async Task<List<TopProductDTO>> GetTopProductsAsync(int top = 5)
        {
            // ✅ Repository xử lý toàn bộ query
            return await _orderRepo.GetTopSellingProductsAsync(top);
        }

        public async Task<List<RecentOrderDTO>> GetRecentOrdersAsync(int count = 10)
        {
            var recentOrders = await _orderRepo.GetRecentOrdersAsync(count);

            // ✅ Mapping DTO có thể giữ ở Service (acceptable)
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
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            switch (reportType.ToLower())
            {
                case "revenue":
                    return await _orderRepo.GetRevenueReportAsync(startDate, adjustedEndDate);

                case "products":
                    return await _orderRepo.GetTopSellingProductsAsync(startDate, adjustedEndDate, 10);

                case "categories":
                    return await _orderRepo.GetCategoryRevenueReportAsync(startDate, adjustedEndDate);

                case "payment_methods":
                    return await _orderRepo.GetRevenueByPaymentMethodAsync(startDate, adjustedEndDate);

                default:
                    return new List<ReportResultDTO>();
            }
        }
    }
}
