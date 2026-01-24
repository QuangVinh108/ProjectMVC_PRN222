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
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtService _jwtService;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtService jwtService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
        }

        public async Task<(string? accessToken, string? refreshToken, string? role)> LoginAsync(string username, string password)
        {
            var user = await _userRepository.AuthenticateAsync(username, password);
            if (user == null) return (null, null, null); // ← Thêm null cho role

            // Revoke old refresh tokens via repository
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.UserId);

            // Generate new tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenString = _jwtService.GenerateRefreshToken();
            var refreshToken = _jwtService.CreateRefreshToken(user, refreshTokenString);

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            Console.WriteLine($"✅ Tokens generated for UserId: {user.UserId}, Role: {user.Role?.RoleName}");

            // ✅ Trả về cả role name
            return (accessToken, refreshTokenString, user.Role?.RoleName);
        }



        public async Task<(string? accessToken, string? refreshToken)?> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenWithUserAsync(refreshToken);

            if (storedToken == null) return null;

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
            var newRefreshTokenString = _jwtService.GenerateRefreshToken();
            var newRefreshToken = _jwtService.CreateRefreshToken(storedToken.User, newRefreshTokenString);

            // Revoke old token
            await _refreshTokenRepository.RevokeTokenAsync(storedToken);
            storedToken.ReplacedByToken = newRefreshTokenString;

            // Add new token
            await _refreshTokenRepository.AddAsync(newRefreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return (newAccessToken, newRefreshTokenString);
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (token == null) return false;

            await _refreshTokenRepository.RevokeTokenAsync(token);
            await _refreshTokenRepository.SaveChangesAsync();

            return true;
        }

        public async Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);

            if (user == null || !user.IsActive)
                return (null, null);

            // Revoke old refresh tokens
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.UserId);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenString = _jwtService.GenerateRefreshToken();
            var refreshToken = _jwtService.CreateRefreshToken(user, refreshTokenString);

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return (accessToken, refreshTokenString);
        }

        public async Task<RegisterResult> RegisterAsync(string username, string email, string password, string fullName)
        {
            try
            {
                Console.WriteLine($"=== REGISTER SERVICE === Username: {username}, Email: {email}");

                // Check username exists via repository
                var existingUsername = await _userRepository.GetByUsernameAsync(username);
                if (existingUsername != null)
                {
                    Console.WriteLine("❌ Username already exists");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "Tên tài khoản đã tồn tại"
                    };
                }

                // Check email exists via repository
                var existingEmail = await _userRepository.GetByEmailAsync(email);
                if (existingEmail != null)
                {
                    Console.WriteLine("❌ Email already exists");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "Email đã được sử dụng"
                    };
                }

                // Get Customer role via repository
                var customerRole = await _userRepository.GetRoleByNameAsync("Customer");
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

                await _userRepository.AddUserAsync(user);
                await _userRepository.SaveChangesAsync();

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
