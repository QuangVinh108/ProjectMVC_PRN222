using BLL.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Trim khoảng trắng + validate cơ bản
            request.Username = request.Username?.Trim();
            request.Password = request.Password?.Trim();

            if (string.IsNullOrWhiteSpace(request.Username))
                ModelState.AddModelError("Username", "Tên tài khoản không được để trống");

            if (string.IsNullOrWhiteSpace(request.Password))
                ModelState.AddModelError("Password", "Mật khẩu không được để trống");

            if (!ModelState.IsValid)
                return BadRequest(ModelState); // JSON lỗi chi tiết

            var (accessToken, refreshToken) = await _authService.LoginAsync(request.Username, request.Password);

            if (accessToken == null)
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không đúng" });

            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (result == null) return Unauthorized("Invalid refresh token");

            return Ok(new { accessToken = result.Value.accessToken, refreshToken = result.Value.refreshToken });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            // Validate refresh token không trống
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token bắt buộc" });

            // Trim và kiểm tra độ dài hợp lý
            request.RefreshToken = request.RefreshToken.Trim();
            if (request.RefreshToken.Length < 10)
                return BadRequest(new { message = "Refresh token không hợp lệ" });

            var success = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
            if (success)
                return Ok(new { message = "Đăng xuất thành công" });

            return NotFound(new { message = "Refresh token không tồn tại" });
        }

    }


    public class LoginRequest
    {
        [Required(ErrorMessage = "Tên tài khoản bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản 3-50 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshRequest
    {
        [Required(ErrorMessage = "Refresh token bắt buộc")]
        [MinLength(10, ErrorMessage = "Refresh token quá ngắn")]
        public string RefreshToken { get; set; } = string.Empty;
    }

}