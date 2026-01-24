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

        public AdminController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
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
    }
}
