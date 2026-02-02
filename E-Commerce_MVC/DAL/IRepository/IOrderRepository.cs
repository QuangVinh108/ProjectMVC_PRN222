using DAL.Entities;

namespace DAL.IRepository
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int orderId, bool includeDetails = false);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task<List<Order>> GetAllAsync();
        Task<Order> CreateAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int orderId);
        Task<bool> ExistsAsync(int orderId);

        //Dashboard
        Task<int> GetTotalOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Order>> GetOrdersThisMonthAsync();
        Task<List<Order>> GetOrdersLastMonthAsync();
        Task<Dictionary<string, int>> GetOrdersByStatusAsync();
        Task<List<Order>> GetRecentOrdersAsync(int count = 10);

        // [MỚI] 1. Báo cáo doanh thu và số lượng đơn theo ngày
        Task<List<ReportResultDTO>> GetRevenueReportAsync(DateTime startDate, DateTime endDate);

        // [MỚI] 2. Top sản phẩm bán chạy nhất (Best Sellers)
        Task<List<ReportResultDTO>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int top = 5);

        // [MỚI] 3. Doanh thu theo danh mục sản phẩm (Category Performance)
        Task<List<ReportResultDTO>> GetCategoryRevenueReportAsync(DateTime startDate, DateTime endDate);

        // [MỚI] 4. Thống kê theo phương thức thanh toán (COD vs Banking)
        Task<List<ReportResultDTO>> GetRevenueByPaymentMethodAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetCompletedRevenueThisMonthAsync();
        Task<decimal> GetCompletedRevenueLastMonthAsync();
        Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);
        Task<List<TopProductDTO>> GetTopSellingProductsAsync(int top);


    }



    public class TopProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int TotalSold { get; set; }  // Tổng số lượng đã bán
        public decimal Revenue { get; set; }  // Tổng doanh thu từ sản phẩm này
    }

    public class ReportResultDTO
    {
        public string Label { get; set; }       // Tên hiển thị (Ngày, Tên SP, Tên Danh mục)
        public decimal Value { get; set; }      // Giá trị (Doanh thu)
        public int Count { get; set; }          // Số lượng (Số đơn, số lượng bán)
        public string ExtraInfo { get; set; }   // Thông tin phụ (VD: Hình ảnh, SKU)
    }
}

