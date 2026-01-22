using Azure;
using Azure.Core;
using BLL.DTOs;
using BLL.Helpers;
using BLL.IService;
using E_Commerce_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IHttpClientFactory _httpClient;
        private readonly IEmailService _emailService; //EMAIL SERVICE

        public AccountController(IAuthService authService,IUserService userService, IHttpClientFactory httpClient)
        public AccountController(IAuthService authService, 
            IGoogleAuthService googleAuthService,
            IHttpClientFactory httpClient,
            IEmailService emailService)
        {
            _authService = authService;
            _userService = userService;
            _googleAuthService = googleAuthService;
            _httpClient = httpClient;
            _emailService = emailService;
        }

        // ==================== LOGIN ====================

        // GET: /Account/Login → Hiển thị form
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            // Nếu đã login, redirect về trang chủ
            var jwtCookie = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(jwtCookie))
            {
                return RedirectToAction("Index", "Product");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login → Xử lý đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Console.WriteLine($"=== LOGIN REQUEST === Username: {model.Username}");

            try
            {
                // Gọi AuthService để xác thực
                var (accessToken, refreshToken) = await _authService.LoginAsync(model.Username, model.Password);

                if (accessToken == null)
                {
                    Console.WriteLine("❌ Login failed - Invalid credentials");

                    // ✅ THÊM LỖI VÀO MODELSTATE - HIỂN THỊ TRÊN FORM
                    ModelState.AddModelError(string.Empty, "Tên tài khoản hoặc mật khẩu không đúng");
                    return View(model);
                }

                Console.WriteLine($"✅ Login successful for {model.Username}");

                // Set JWT cookie
                Response.Cookies.Append("jwt", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddMinutes(25)
                });

                // Optional: Set refresh token
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });
                }

                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                return LocalRedirect(model.ReturnUrl ?? "/Product");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in Login: {ex.Message}");

                // ✅ THÊM LỖI VÀO MODELSTATE
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
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
        // ==================== GOOGLE LOGIN ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GoogleLogin(string idToken, string? returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                TempData["ErrorMessage"] = "Lỗi xác thực Google";
                return RedirectToAction("Login");
            }

            try
            {
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

                // Set refresh token cookie
                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(7)
                    });
                }

                TempData["SuccessMessage"] = result.Message;
                return LocalRedirect(returnUrl ?? "/Product");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in GoogleLogin: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đăng nhập Google";
                return RedirectToAction("Login");
            }
        }

        // ==================== LOGOUT ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refresh_token");
            TempData.Clear();

            TempData["SuccessMessage"] = "Đăng xuất thành công!";
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

        // ==================== REGISTER ====================

        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã login, redirect về Product
            var jwtCookie = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(jwtCookie))
            {
                return RedirectToAction("Index", "Product");
            }

            return View(new RegisterViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOtp([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return Json(new { success = false, message = "Email không hợp lệ" });
            }

            try
            {
                Console.WriteLine($"=== SEND OTP REQUEST === Email: {email}");

                // Tạo OTP 6 số
                var otp = OtpHelper.GenerateOtp();

                // Lưu OTP (5 phút)
                OtpHelper.StoreOtp(email, otp, 5);

                // Gửi email
                var sent = await _emailService.SendOtpEmailAsync(email, otp);

                if (sent)
                {
                    Console.WriteLine($"✅ OTP sent successfully to {email}");
                    return Json(new { success = true, message = "Mã OTP đã được gửi đến email của bạn" });
                }
                else
                {
                    Console.WriteLine($"❌ Failed to send OTP to {email}");
                    OtpHelper.RemoveOtp(email); // Xóa OTP nếu gửi email thất bại
                    return Json(new { success = false, message = "Không thể gửi email. Vui lòng thử lại" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SendOtp Error: {ex.Message}");
                return Json(new { success = false, message = "Đã xảy ra lỗi khi gửi OTP" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                Console.WriteLine($"=== REGISTER REQUEST === Username: {model.Username}, Email: {model.Email}");

                // ✅ VALIDATE OTP
                if (string.IsNullOrEmpty(model.OtpCode))
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng nhập mã OTP");
                    return View(model);
                }

                // Kiểm tra OTP
                if (!OtpHelper.ValidateOtp(model.Email, model.OtpCode))
                {
                    if (OtpHelper.IsOtpExpired(model.Email))
                    {
                        ModelState.AddModelError(string.Empty, "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Mã OTP không chính xác");
                    }
                    return View(model);
                }

                Console.WriteLine($"✅ OTP validated for {model.Email}");

                // GỌI AUTHSERVICE ĐỂ ĐĂNG KÝ
                var result = await _authService.RegisterAsync(
                    model.Username,
                    model.Email,
                    model.Password,
                    model.FullName
                );

                if (result.Success)
                {
                    Console.WriteLine($"✅ Register successful - UserId: {result.UserId}");
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    Console.WriteLine($"❌ Register failed: {result.Message}");
                    ModelState.AddModelError(string.Empty, result.Message ?? "Đăng ký thất bại");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in Register: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.");
                return View(model);
            }
        }
    }
}
