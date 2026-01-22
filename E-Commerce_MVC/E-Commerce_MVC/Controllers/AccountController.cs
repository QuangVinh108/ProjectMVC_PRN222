using Azure;
using Azure.Core;
using BLL.DTOs;
using BLL.IService;
using E_Commerce_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClient;

        public AccountController(IAuthService authService,IUserService userService, IHttpClientFactory httpClient)
        {
            _authService = authService;
            _userService = userService;
            _httpClient = httpClient;
        }

        // GET: /Account/Login → Hiển thị form
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login → Gọi API + set cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Gọi API AuthController
            var (accessToken, refreshToken) = await _authService.LoginAsync(model.Username, model.Password);
            if (accessToken == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng");
                return View(model);
            }

            // Set JWT cookie → MVC View dùng được [Authorize]
            Response.Cookies.Append("jwt", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(25) // < 30p expiry
            });

            // Redirect trang mong muốn
            return LocalRedirect(model.ReturnUrl ?? "/Product");
        }

        [HttpGet]
        [Authorize] // Bắt buộc phải đăng nhập mới xem được
        public IActionResult Profile()
        {
            // Lấy User ID từ Cookie (Token)
            // Lưu ý: ClaimTypes.NameIdentifier thường là UserId nếu bạn cấu hình chuẩn trong AuthService
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Nếu không lấy được ID, bắt đăng nhập lại
                return RedirectToAction("Login");
            }

            int userId = int.Parse(userIdClaim);

            // Gọi Service lấy thông tin User
            var user = _userService.GetUserById(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: /Account/Logout → Xóa cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult EditProfile()
        {
            // 1. Lấy User ID hiện tại
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login");
            int userId = int.Parse(userIdClaim);

            // 2. Lấy thông tin user từ Service
            var user = _userService.GetUserById(userId);
            if (user == null) 
                return NotFound();

            // 3. Map sang ViewModel để hiển thị lên form
            var model = new UpdateProfileViewModel
            {
                Email = user.Email, 
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address
            };

            return View(model);
        }

        // --- THÊM MỚI: Xử lý lưu cập nhật ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Trả về form nếu có lỗi validation
            }

            try
            {
                // 1. Lấy User ID hiện tại
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
                if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login");
                int userId = int.Parse(userIdClaim);

                // 2. Gọi Service để cập nhật
                _userService.UpdateProfile(userId, model);

                // 3. Thông báo và chuyển hướng về trang Profile
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View(model);
            }
        }
    }
}