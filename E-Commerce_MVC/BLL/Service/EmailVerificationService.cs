using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailVerificationTokenRepository _tokenRepository;

        public EmailVerificationService(
            IUserRepository userRepository,
            IEmailVerificationTokenRepository tokenRepository)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
        }

        public async Task<VerificationResult> SendVerificationEmailAsync(int userId, string email)
        {
            // Kiểm tra user tồn tại qua repository
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new VerificationResult
                {
                    Success = false,
                    Message = "User không tồn tại"
                };
            }

            // TRƯỜNG HỢP 3: Check email đã dùng bởi Google account
            var existingGoogleUser = await _userRepository
                .FindGoogleUserByEmailExcludingUserIdAsync(email, userId);

            if (existingGoogleUser != null)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.EmailOwnedByGoogleAccount,
                    Message = $"Email {email} đã được sử dụng bởi tài khoản Google khác.",
                    ConflictEmail = email
                };
            }

            // Check email đã được verified bởi user khác
            var existingVerifiedUser = await _userRepository
                .FindVerifiedUserByEmailExcludingUserIdAsync(email, userId);

            if (existingVerifiedUser != null)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.EmailAlreadyTaken,
                    Message = $"Email {email} đã được sử dụng.",
                    ConflictEmail = email
                };
            }

            // Generate token và lưu qua repository
            var token = GenerateSecureToken();
            var verificationToken = new EmailVerificationToken
            {
                UserId = userId,
                Token = token,
                Email = email,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            };

            await _tokenRepository.AddAsync(verificationToken);
            await _tokenRepository.SaveChangesAsync();

            return new VerificationResult
            {
                Success = true,
                Message = $"Email verification sent. Token: {token}"
            };
        }

        public async Task<VerificationResult> VerifyEmailTokenAsync(string token)
        {
            // Lấy token với user qua repository
            var verificationToken = await _tokenRepository.GetByTokenWithUserAsync(token);

            if (verificationToken == null)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.TokenInvalid,
                    Message = "Token không hợp lệ"
                };
            }

            // Validate token
            if (verificationToken.IsUsed)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.TokenAlreadyUsed,
                    Message = "Token đã được sử dụng"
                };
            }

            if (verificationToken.ExpiresAt < DateTime.UtcNow)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.TokenExpired,
                    Message = "Token đã hết hạn"
                };
            }

            // TRƯỜNG HỢP 3: Check lại conflict với Google account
            var conflictGoogleUser = await _userRepository
                .FindGoogleUserByEmailExcludingUserIdAsync(
                    verificationToken.Email,
                    verificationToken.UserId);

            if (conflictGoogleUser != null)
            {
                verificationToken.IsUsed = true;
                await _tokenRepository.SaveChangesAsync();

                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.EmailOwnedByGoogleAccount,
                    Message = $"Email đã được tài khoản Google khác sử dụng.",
                    ConflictEmail = verificationToken.Email
                };
            }

            // Update user email và confirm status
            var user = verificationToken.User;
            if (user != null)
            {
                user.Email = verificationToken.Email;
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                verificationToken.IsUsed = true;

                await _userRepository.SaveChangesAsync();

                return new VerificationResult
                {
                    Success = true,
                    Message = "Email đã được xác thực thành công"
                };
            }

            return new VerificationResult
            {
                Success = false,
                ErrorType = VerificationErrorType.TokenInvalid,
                Message = "Không tìm thấy user"
            };
        }

        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
