using BLL.IService;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<AdminController> _logger;
        public AdminController(IDashboardService dashboardService, ILogger<AdminController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // ✅ KIỂM TRA XEM ACTION NÀY CÓ ĐÚNG KHÔNG
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardStatisticsAsync();

                // ✅ THÊM LOGGING ĐỂ DEBUG
                if (stats == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu dashboard";
                    return View(new BLL.DTOs.DashboardStatisticsDTO()); // Trả về empty model
                }

                return View(stats);
            }
            catch (Exception ex)
            {
                // ✅ BẮT LỖI VÀ LOG
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return View(new BLL.DTOs.DashboardStatisticsDTO());
            }
        }

        public IActionResult Report()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        // ✅ API ENDPOINTS
        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData(int days = 30)
        {
            try
            {
                var data = await _dashboardService.GetRevenueChartDataAsync(days);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusChartData()
        {
            try
            {
                var data = await _dashboardService.GetOrderStatusChartAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserGrowthChartData(int months = 6)
        {
            try
            {
                var data = await _dashboardService.GetUserGrowthChartAsync(months);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTopProducts()
        {
            try
            {
                var data = await _dashboardService.GetTopProductsAsync(5);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentOrders()
        {
            try
            {
                var data = await _dashboardService.GetRecentOrdersAsync(10);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        // ==========================================
        // REPORTING API (Phục vụ trang Báo cáo)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetReportData(DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                // Validate dữ liệu
                if (startDate == DateTime.MinValue || endDate == DateTime.MinValue)
                    return Json(new { success = false, message = "Vui lòng chọn khoảng thời gian hợp lệ." });

                if (startDate > endDate)
                    return Json(new { success = false, message = "Ngày bắt đầu không được lớn hơn ngày kết thúc." });

                // Gọi Service
                var data = await _dashboardService.GetReportDataAsync(startDate, endDate, reportType);

                return Json(new { success = true, data = data, type = reportType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetReportData: {Type} từ {Start} đến {End}", reportType, startDate, endDate);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi tạo báo cáo." });
            }
        }

        // ==========================================
        // EXPORT ACTIONS (Bổ sung cho tương lai)
        // ==========================================

        // [MỚI] Action để xuất Excel (Cần cài thêm thư viện như EPPlus hoặc ClosedXML)
        [HttpGet]
        public async Task<IActionResult> ExportReportToExcel(DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                // TODO: Implement logic tạo file Excel tại đây
                // var fileContent = await _dashboardService.ExportToExcelAsync(...);
                // return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{reportType}_{DateTime.Now.Ticks}.xlsx");

                await Task.Delay(100); // Giả lập xử lý
                return BadRequest("Tính năng đang được phát triển.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi Export Excel");
                return BadRequest("Lỗi khi xuất file.");
            }
        }
    }
}
