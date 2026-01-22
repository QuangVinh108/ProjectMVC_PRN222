using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.DTOs;
namespace BLL.IService
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken);
        Task<GoogleAuthResult> HandleGoogleLoginAsync(GoogleUserInfo googleUser);
    }
}
