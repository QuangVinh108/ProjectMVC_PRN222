using System.ComponentModel.DataAnnotations;

namespace E_Commerce_MVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên tài khoản bắt buộc")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Tên tài khoản")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu bắt buộc")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
