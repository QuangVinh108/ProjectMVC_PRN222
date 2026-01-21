using BLL.IService;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DAL.IRepository;

namespace BLL.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ShopDbContext _context;

        public AuthService(IUserRepository userRepository, IJwtService jwtService, ShopDbContext context)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _context = context;
        }

        public async Task<(string? accessToken, string? refreshToken)> LoginAsync(string username, string password)
        {
            var user = await _userRepository.AuthenticateAsync(username, password);
            if (user == null) return (null, null);

            // Revoke old refresh tokens
            var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.UserId && !rt.IsRevoked);
            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenString = _jwtService.GenerateRefreshToken();
            var refreshToken = _jwtService.CreateRefreshToken(user, refreshTokenString);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return (accessToken, refreshTokenString);
        }

        public async Task<(string? accessToken, string? refreshToken)?> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

            if (storedToken == null) return null;

            var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
            var newRefreshTokenString = _jwtService.GenerateRefreshToken();
            var newRefreshToken = _jwtService.CreateRefreshToken(storedToken.User, newRefreshTokenString);

            // Revoke old token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshTokenString;

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return (newAccessToken, newRefreshTokenString);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null) return false;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(int userId)
        {
            // Lấy user với Role để generate JWT
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || !user.IsActive)
                return (null, null);

            // Revoke old refresh tokens (giống logic LoginAsync)
            var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.UserId && !rt.IsRevoked);
            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            // Generate tokens bằng JwtService (giống logic LoginAsync)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenString = _jwtService.GenerateRefreshToken();
            var refreshToken = _jwtService.CreateRefreshToken(user, refreshTokenString);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return (accessToken, refreshTokenString);
        }

    }
}
