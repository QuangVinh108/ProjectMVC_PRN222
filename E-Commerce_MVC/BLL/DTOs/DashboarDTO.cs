using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class DashboardStatisticsDTO
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int OrdersThisMonth { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal AverageOrderValue { get; set; }

        // % tăng trưởng so với tháng trước
        public double RevenueGrowthPercent { get; set; }
        public double OrderGrowthPercent { get; set; }
        public double UserGrowthPercent { get; set; }
    }

    // ✅ Dữ liệu biểu đồ doanh thu
    public class RevenueChartDTO
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Data { get; set; } = new();
    }

    // ✅ Top sản phẩm bán chạy
    public class TopProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Image { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }

    // ✅ Đơn hàng gần đây
    public class RecentOrderDTO
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ✅ Biểu đồ trạng thái đơn hàng
    public class OrderStatusChartDTO
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Data { get; set; } = new();
    }

    // ✅ Tăng trưởng user theo tháng
    public class UserGrowthChartDTO
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Data { get; set; } = new();
    }
}
