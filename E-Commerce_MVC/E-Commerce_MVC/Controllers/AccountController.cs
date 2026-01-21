using Azure;
using Azure.Core;
using BLL.IService;
using E_Commerce_MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IGoogleAuthService _googleAuthService; // THÊM
        private readonly IHttpClientFactory _httpClient;

        public AccountController(IAuthService authService, IGoogleAuthService googleAuthService, IHttpClientFactory httpClient)
        {
            _authService = authService;
            _googleAuthService = googleAuthService; // THÊM
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLogin(string idToken, string? returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                TempData["ErrorMessage"] = "Lỗi xác thực Google";
                return RedirectToAction("Login");
            }

            // Verify Google token
            var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(idToken);
            if (googleUser == null)
            {
                TempData["ErrorMessage"] = "Google token không hợp lệ";
                return RedirectToAction("Login");
            }

            // Handle login
            var result = await _googleAuthService.HandleGoogleLoginAsync(googleUser);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message ?? "Đăng nhập Google thất bại";
                return RedirectToAction("Login");
            }

            // Set JWT cookie
            Response.Cookies.Append("jwt", result.AccessToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(25)
            });

            // Optional: Set refresh token cookie
            Response.Cookies.Append("refresh_token", result.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            TempData["SuccessMessage"] = result.Message;
            return LocalRedirect(returnUrl ?? "/Product");
        }


        // POST: /Account/Logout → Xóa cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refresh_token");

            // XÓA TEMPDDATA CỦ - QUAN TRỌNG!
            TempData.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}