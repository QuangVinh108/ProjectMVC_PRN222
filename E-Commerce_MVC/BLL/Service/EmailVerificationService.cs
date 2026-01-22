using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
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
        private readonly ShopDbContext _context;

        public EmailVerificationService(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<VerificationResult> SendVerificationEmailAsync(int userId, string email)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new VerificationResult
                {
                    Success = false,
                    Message = "User không tồn tại"
                };
            }

            // TRƯỜNG HỢP 3
            var existingGoogleUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email
                                       && u.GoogleId != null
                                       && u.EmailConfirmed
                                       && u.UserId != userId);

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

            var existingVerifiedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email
                                       && u.EmailConfirmed
                                       && u.UserId != userId);

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

            _context.EmailVerificationTokens.Add(verificationToken);
            await _context.SaveChangesAsync();

            return new VerificationResult
            {
                Success = true,
                Message = $"Email verification sent. Token: {token}"
            };
        }

        public async Task<VerificationResult> VerifyEmailTokenAsync(string token)
        {
            var verificationToken = await _context.EmailVerificationTokens
                .Include(vt => vt.User)
                .FirstOrDefaultAsync(vt => vt.Token == token);

            if (verificationToken == null)
            {
                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.TokenInvalid,
                    Message = "Token không hợp lệ"
                };
            }

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

            // TRƯỜNG HỢP 3 check lại
            var conflictGoogleUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == verificationToken.Email
                                       && u.GoogleId != null
                                       && u.EmailConfirmed
                                       && u.UserId != verificationToken.UserId);

            if (conflictGoogleUser != null)
            {
                verificationToken.IsUsed = true;
                await _context.SaveChangesAsync();

                return new VerificationResult
                {
                    Success = false,
                    ErrorType = VerificationErrorType.EmailOwnedByGoogleAccount,
                    Message = $"Email đã được tài khoản Google khác sử dụng.",
                    ConflictEmail = verificationToken.Email
                };
            }

            var user = verificationToken.User;
            if (user != null)
            {
                user.Email = verificationToken.Email;
                user.EmailConfirmed = true;
                user.EmailConfirmedAt = DateTime.UtcNow;
                verificationToken.IsUsed = true;
                await _context.SaveChangesAsync();

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
