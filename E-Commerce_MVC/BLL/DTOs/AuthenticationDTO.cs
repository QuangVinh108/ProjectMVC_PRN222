using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
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
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Tên tài khoản bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản 3-50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Chỉ chứa chữ, số và dấu gạch dưới")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên 2-100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; } = string.Empty;
    }
}
