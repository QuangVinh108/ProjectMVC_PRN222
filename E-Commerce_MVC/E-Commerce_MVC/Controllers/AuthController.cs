//using BLL.IService;
//using BLL.Service;
//using E_Commerce_MVC.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.ComponentModel.DataAnnotations;
//using BLL.DTOs;
//namespace E_Commerce_MVC.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : ControllerBase
//    {
//        private readonly IAuthService _authService;
//        private readonly IGoogleAuthService _googleAuthService; // new

//        public AuthController(IAuthService authService, IGoogleAuthService googleAuthService)
//        {
//            _authService = authService;
//            _googleAuthService = googleAuthService; // new
//        }

//        [HttpPost("login")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Login([FromBody] LoginRequest request)
//        {
//            // Trim khoảng trắng + validate cơ bản
//            request.Username = request.Username?.Trim();
//            request.Password = request.Password?.Trim();

//            if (string.IsNullOrWhiteSpace(request.Username))
//                ModelState.AddModelError("Username", "Tên tài khoản không được để trống");

//            if (string.IsNullOrWhiteSpace(request.Password))
//                ModelState.AddModelError("Password", "Mật khẩu không được để trống");

//            if (!ModelState.IsValid)
//                return BadRequest(ModelState); // JSON lỗi chi tiết

//            var (accessToken, refreshToken) = await _authService.LoginAsync(request.Username, request.Password);

//            if (accessToken == null)
//                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không đúng" });

//            return Ok(new { accessToken, refreshToken });
//        }

//        [HttpPost("google-login")]
//        [AllowAnonymous]
//        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
//        {
//            if (string.IsNullOrWhiteSpace(request.IdToken))
//                return BadRequest(new { message = "Google ID Token bắt buộc" });

//            // Verify Google token
//            var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
//            if (googleUser == null)
//            {
//                return Unauthorized(new { message = "Google token không hợp lệ hoặc đã hết hạn" });
//            }

//            // Handle login logic với 3 trường hợp
//            var result = await _googleAuthService.HandleGoogleLoginAsync(googleUser);

//            if (!result.Success)
//            {
//                // TRƯỜNG HỢP 3: Email conflict
//                if (result.ErrorType == BLL.DTOs.GoogleAuthErrorType.EmailNotVerifiedByGoogle)
//                {
//                    return BadRequest(new
//                    {
//                        message = result.Message,
//                        errorType = "EMAIL_NOT_VERIFIED"
//                    });
//                }

//                return BadRequest(new { message = result.Message });
//            }

//            // Thành công - trả về tokens
//            return Ok(new
//            {
//                accessToken = result.AccessToken,
//                refreshToken = result.RefreshToken,
//                message = result.Message
//            });
//        }

//        [HttpPost("refresh")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
//        {
//            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
//            if (result == null) return Unauthorized("Invalid refresh token");

//            return Ok(new { accessToken = result.Value.accessToken, refreshToken = result.Value.refreshToken });
//        }

//        [HttpPost("logout")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
//        {
//            // Validate refresh token không trống
//            if (string.IsNullOrWhiteSpace(request.RefreshToken))
//                return BadRequest(new { message = "Refresh token bắt buộc" });

//            // Trim và kiểm tra độ dài hợp lý
//            request.RefreshToken = request.RefreshToken.Trim();
//            if (request.RefreshToken.Length < 10)
//                return BadRequest(new { message = "Refresh token không hợp lệ" });

//            var success = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
//            if (success)
//                return Ok(new { message = "Đăng xuất thành công" });

//            return NotFound(new { message = "Refresh token không tồn tại" });
//        }
//        [HttpPost("register")]
//        [AllowAnonymous]
//        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
//        {
//            Console.WriteLine($"=== REGISTER API === Username: {request.Username}, Email: {request.Email}");

//            // Validate
//            request.Username = request.Username?.Trim();
//            request.Email = request.Email?.Trim();
//            request.FullName = request.FullName?.Trim();

//            if (!ModelState.IsValid)
//            {
//                Console.WriteLine("❌ Model validation failed");
//                return BadRequest(ModelState);
//            }

//            try
//            {
//                // Gọi AuthService để đăng ký
//                var result = await _authService.RegisterAsync(
//                    request.Username,
//                    request.Email,
//                    request.Password,
//                    request.FullName
//                );

//                if (result.Success)
//                {
//                    Console.WriteLine($"✅ Register successful for {request.Username}");
//                    return Ok(new { message = result.Message ?? "Đăng ký thành công" });
//                }
//                else
//                {
//                    Console.WriteLine($"❌ Register failed: {result.Message}");
//                    return BadRequest(new { message = result.Message ?? "Đăng ký thất bại" });
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Exception in Register: {ex.Message}");
//                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đăng ký" });
//            }
//        }



//    }



//}