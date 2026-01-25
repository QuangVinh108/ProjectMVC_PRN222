using Azure;
using Azure.Core;
using BLL.DTOs;
using BLL.Helper;
using BLL.Helpers;
using BLL.IService;
using E_Commerce_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;

namespace E_Commerce_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IHttpClientFactory _httpClient;
        private readonly IEmailService _emailService; //EMAIL SERVICE
        private readonly GeminiHelper _geminiHelper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(IAuthService authService,
            IUserService userService, 
            IGoogleAuthService googleAuthService,
            IHttpClientFactory httpClient,
            IEmailService emailService,
            GeminiHelper geminiHelper,          
            IWebHostEnvironment webHostEnvironment)
        {
            _authService = authService;
            _userService = userService;
            _googleAuthService = googleAuthService;
            _httpClient = httpClient;
            _emailService = emailService;
            _geminiHelper = geminiHelper;       
            _webHostEnvironment = webHostEnvironment; 
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
        // ✅ GỌI AuthService - BÂY GIỜ NHẬN CẢ ROLE
        var (accessToken, refreshToken, role) = await _authService.LoginAsync(model.Username, model.Password);
        
        if (accessToken == null)
        {
            Console.WriteLine("❌ Login failed - Invalid credentials");
            ModelState.AddModelError(string.Empty, "Tên tài khoản hoặc mật khẩu không đúng");
            return View(model);
        }

        Console.WriteLine($"✅ Login successful for {model.Username} - Role: {role}");
        
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

        // ✅ REDIRECT THEO ROLE
        if (!string.IsNullOrEmpty(role))
        {
            switch (role.ToLower())
            {
                case "admin":
                    return RedirectToAction("Dashboard", "Admin");
                
                case "customer":
                default:
                    // Ưu tiên returnUrl nếu có, không thì về Product
                    return LocalRedirect(model.ReturnUrl ?? "/Product");
            }
        }

        // Fallback
        return LocalRedirect(model.ReturnUrl ?? "/Product");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception in Login: {ex.Message}");
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

                // ✅ REDIRECT THEO ROLE (nếu GoogleAuthResult có Role)
                // Bạn cần thêm property Role vào GoogleAuthResult hoặc lấy từ JWT
                if (!string.IsNullOrEmpty(result.Role))
                {
                    switch (result.Role.ToLower())
                    {
                        case "admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "manager":
                            return RedirectToAction("Dashboard", "Manager");
                        default:
                            return LocalRedirect(returnUrl ?? "/Product");
                    }
                }

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

            ViewBag.IsVerified = user.IsIdentityVerified;

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

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // 2. POST: Xử lý đổi mật khẩu
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Lấy UserId từ Cookie
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
                int userId = int.Parse(userIdClaim);

                // Gọi Service xử lý (Chúng ta sẽ viết hàm này ở Bước 4)
                var result = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Index", "Home"); // Hoặc về Home
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.Message); // Hiển thị lỗi từ Service (vd: Sai pass cũ)
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult VerifyIdentity()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
            int userId = int.Parse(userIdClaim);
            var user = _userService.GetUserById(userId);

            // Nếu đã xác thực rồi -> chặn
            if (user.IsIdentityVerified) return RedirectToAction("Profile");

            // Load thông tin hiện tại của User lên form để họ sửa nếu cần
            var model = new EkycRequestViewModel
            {
                FullName = user.FullName,
                CccdNumber = user.CccdNumber,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth ?? DateTime.Today // Mặc định nếu chưa có
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyIdentity(EkycRequestViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // 1. Gọi AI phân tích ảnh
                var aiResult = await _geminiHelper.AnalyzeIdCardAsync(model.FrontImage);

                if (aiResult == null)
                {
                    ModelState.AddModelError("", "Lỗi kết nối AI. Vui lòng thử lại.");
                    return View(model);
                }

                if (!aiResult.IsValid)
                {
                    ModelState.AddModelError("", $"Ảnh không hợp lệ: {aiResult.Reason}");
                    return View(model);
                }

                // ========================================================
                // 2. LOGIC MATCHING (SO SÁNH THÔNG TIN) - PHẦN BẠN CẦN
                // ========================================================

                // 2.1. So khớp Số CCCD (Phải giống tuyệt đối)
                if (model.CccdNumber.Trim() != aiResult.Data.IdNumber.Trim())
                {
                    ModelState.AddModelError("CccdNumber", $"Số CCCD bạn nhập ({model.CccdNumber}) không khớp với ảnh ({aiResult.Data.IdNumber}).");
                    return View(model);
                }

                // 2.2. So khớp Họ tên (Dùng Helper để so sánh tương đối: bỏ dấu, chữ thường)
                string inputName = StringHelper.NormalizeString(model.FullName);
                string aiName = StringHelper.NormalizeString(aiResult.Data.FullName);

                // Chấp nhận sai khác nhỏ hoặc bắt buộc chính xác 100% tùy bạn.
                // Ở đây tôi dùng Contains hoặc so sánh bằng
                if (inputName != aiName)
                {
                    ModelState.AddModelError("FullName", $"Họ tên nhập vào không khớp với trên thẻ.\nNhập: {model.FullName}\nThẻ: {aiResult.Data.FullName}");
                    return View(model);
                }

                // 2.3. So khớp Ngày sinh
                if (!StringHelper.CompareDates(model.DateOfBirth, aiResult.Data.Dob))
                {
                    ModelState.AddModelError("DateOfBirth", $"Ngày sinh không khớp. Trên thẻ là: {aiResult.Data.Dob}");
                    return View(model);
                }

                // ========================================================
                // 3. NẾU KHỚP HẾT -> TIẾN HÀNH NÂNG CẤP USER
                // ========================================================

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("Id");
                int userId = int.Parse(userIdClaim);
                var user = _userService.GetUserById(userId);

                // Lưu ảnh
                string uniqueFileName = $"KYC_{userId}_{Guid.NewGuid()}_{Path.GetExtension(model.FrontImage.FileName)}";

                // 1. Xác định đường dẫn thư mục chứa ảnh
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "kyc");

                // === 🔥 QUAN TRỌNG: THÊM ĐOẠN NÀY ĐỂ SỬA LỖI 🔥 ===
                // Kiểm tra nếu thư mục chưa tồn tại thì tạo mới
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                // ===================================================

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.FrontImage.CopyToAsync(stream);
                }

                // Cập nhật thông tin (Ưu tiên lấy thông tin từ AI để chuẩn hóa dữ liệu lưu vào DB)
                user.CccdNumber = aiResult.Data.IdNumber;
                user.FullName = aiResult.Data.FullName; // Lấy tên in hoa từ thẻ cho đẹp
                user.DateOfBirth = model.DateOfBirth;
                user.Address = aiResult.Data.Address; // Địa chỉ lấy từ thẻ luôn cho chính xác
                user.CccdFrontImage = "/images/kyc/" + uniqueFileName;

                // Nâng cấp trạng thái
                user.IsIdentityVerified = true;
                user.IdentityRejectReason = null;

                _userService.UpdateUser(user);

                TempData["SuccessMessage"] = "Xác thực thành công! Thông tin đã được đối chiếu và cập nhật.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                return View(model);
            }
        }   
    }
}
