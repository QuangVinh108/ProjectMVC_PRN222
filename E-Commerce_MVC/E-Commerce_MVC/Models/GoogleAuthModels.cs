using System.ComponentModel.DataAnnotations;

namespace E_Commerce_MVC.Models
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID Token bắt buộc")]
        public string IdToken { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class SendVerificationRequest
    {
        [Required(ErrorMessage = "Email bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
