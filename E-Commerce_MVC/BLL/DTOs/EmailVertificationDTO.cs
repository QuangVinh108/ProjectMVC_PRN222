using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
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

    public class VerificationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public VerificationErrorType ErrorType { get; set; }
        public string? ConflictEmail { get; set; }
    }

    public enum VerificationErrorType
    {
        None,
        EmailOwnedByGoogleAccount, // TRƯỜNG HỢP 3
        EmailAlreadyTaken,
        TokenExpired,
        TokenInvalid,
        TokenAlreadyUsed
    }
}
