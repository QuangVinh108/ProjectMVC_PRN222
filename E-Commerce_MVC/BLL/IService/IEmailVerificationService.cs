using BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IEmailVerificationService
    {
        Task<VerificationResult> SendVerificationEmailAsync(int userId, string email);
        Task<VerificationResult> VerifyEmailTokenAsync(string token);
    }
}
