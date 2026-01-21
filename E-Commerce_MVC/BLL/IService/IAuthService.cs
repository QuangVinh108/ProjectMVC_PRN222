using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IAuthService
    {
        Task<(string? accessToken, string? refreshToken)> LoginAsync(string username, string password);
        Task<(string? accessToken, string? refreshToken)?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(int userId);
    }
}

