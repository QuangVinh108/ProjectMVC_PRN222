using DAL.Entities;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ShopDbContext _context;

        public RefreshTokenRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken?> GetByTokenWithUserAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == token
                    && !rt.IsRevoked
                    && rt.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();
        }

        public async Task AddAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task RevokeTokenAsync(RefreshToken token)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await GetActiveTokensByUserIdAsync(userId);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
