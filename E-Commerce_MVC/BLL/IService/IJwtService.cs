using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        bool ValidateAccessToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
        RefreshToken? CreateRefreshToken(User user, string refreshToken);
    }
}
