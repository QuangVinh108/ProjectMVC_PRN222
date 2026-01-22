using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine($"✅ Tokens generated for UserId: {user.UserId}");

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

        public async Task<RegisterResult> RegisterAsync(string username, string email, string password, string fullName)
        {
            try
            {
                Console.WriteLine($"=== REGISTER SERVICE === Username: {username}, Email: {email}");

                // Check username exists
                var existingUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (existingUsername != null)
                {
                    Console.WriteLine("❌ Username already exists");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "Tên tài khoản đã tồn tại"
                    };
                }

                // Check email exists
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingEmail != null)
                {
                    Console.WriteLine("❌ Email already exists");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "Email đã được sử dụng"
                    };
                }

                // Get Customer role
                var customerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Customer");

                if (customerRole == null)
                {
                    Console.WriteLine("❌ Customer role not found");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "Lỗi hệ thống: không tìm thấy role Customer"
                    };
                }

                // Create user
                var user = new User
                {
                    UserName = username,
                    Email = email,
                    FullName = fullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = customerRole.RoleId,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    LoginProvider = "Local"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ User created with UserId: {user.UserId}");

                // TODO: Send verification email

                return new RegisterResult
                {
                    Success = true,
                    Message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.",
                    UserId = user.UserId
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in RegisterAsync: {ex.Message}");
                return new RegisterResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đăng ký"
                };
            }
        }

    }
}
