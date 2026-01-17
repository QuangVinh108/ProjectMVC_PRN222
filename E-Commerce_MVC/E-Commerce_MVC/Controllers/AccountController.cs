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
        private readonly IHttpClientFactory _httpClient;

        public AccountController(IAuthService authService, IHttpClientFactory httpClient)
        {
            _authService = authService;
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

        // POST: /Account/Logout → Xóa cookie
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Home");
        }
    }
}