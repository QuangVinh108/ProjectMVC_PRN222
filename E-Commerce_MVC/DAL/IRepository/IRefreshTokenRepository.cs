using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepository
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetByTokenWithUserAsync(string token);
        Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId);
        Task AddAsync(RefreshToken refreshToken);
        Task RevokeTokenAsync(RefreshToken token);
        Task RevokeAllUserTokensAsync(int userId);
        Task SaveChangesAsync();
    }
}
