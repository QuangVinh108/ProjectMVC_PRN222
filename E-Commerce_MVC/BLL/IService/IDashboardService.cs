using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
namespace BLL.IService
{
    public interface IDashboardService
    {
        Task<DashboardStatisticsDTO> GetDashboardStatisticsAsync();
        Task<RevenueChartDTO> GetRevenueChartDataAsync(int days = 30);
        Task<List<TopProductDTO>> GetTopProductsAsync(int top = 5);
        Task<List<RecentOrderDTO>> GetRecentOrdersAsync(int count = 10);
        Task<OrderStatusChartDTO> GetOrderStatusChartAsync();
        Task<UserGrowthChartDTO> GetUserGrowthChartAsync(int months = 6);
    }
}
