using BLL.DTOs;
using BLL.IService;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BLL.Service
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly ShopDbContext _context;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public GoogleAuthService(
            ShopDbContext context,
            IAuthService authService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _authService = authService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                Console.WriteLine("=== VERIFYING GOOGLE TOKEN ===");
                Console.WriteLine($"Token length: {idToken?.Length ?? 0}");

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(
                    $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}"
                );

                Console.WriteLine($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Google API Error: {errorContent}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Google Response (first 400 chars): {json.Substring(0, Math.Min(400, json.Length))}...");

                // Parse JSON manually để tránh lỗi type conversion
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var googleUser = new GoogleUserInfo
                {
                    Sub = root.GetProperty("sub").GetString() ?? "",
                    Email = root.GetProperty("email").GetString() ?? "",
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                    GivenName = root.TryGetProperty("given_name", out var givenName) ? givenName.GetString() ?? "" : "",
                    FamilyName = root.TryGetProperty("family_name", out var familyName) ? familyName.GetString() ?? "" : "",
                    Picture = root.TryGetProperty("picture", out var picture) ? picture.GetString() ?? "" : "",
                    Aud = root.GetProperty("aud").GetString() ?? "",

                    // Parse email_verified safely - handle both boolean and string
                    EmailVerified = root.TryGetProperty("email_verified", out var emailVerified)
                        ? emailVerified.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => emailVerified.GetString() == "true",
                            _ => false
                        }
                        : false
                };

                Console.WriteLine($"Deserialized - Email: {googleUser.Email}, Sub: {googleUser.Sub}, EmailVerified: {googleUser.EmailVerified}");

                // Verify Client ID
                var expectedClientId = _configuration["Google:ClientId"];
                Console.WriteLine($"Expected Client ID: {expectedClientId}");
                Console.WriteLine($"Token aud: {googleUser.Aud}");

                if (string.IsNullOrEmpty(expectedClientId))
                {
                    Console.WriteLine("❌ Expected Client ID is NULL!");
                    return null;
                }

                if (googleUser.Aud != expectedClientId)
                {
                    Console.WriteLine($"❌ Client ID mismatch!");
                    return null;
                }

                Console.WriteLine("✅ Token verified successfully!");
                Console.WriteLine($"   Email: {googleUser.Email}");
                Console.WriteLine($"   Name: {googleUser.Name}");
                Console.WriteLine($"   EmailVerified: {googleUser.EmailVerified}");

                return googleUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION in VerifyGoogleTokenAsync:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }

                return null;
            }
        }


        public async Task<GoogleAuthResult> HandleGoogleLoginAsync(GoogleUserInfo googleUser)
        {
            Console.WriteLine("=== HANDLE GOOGLE LOGIN ===");
            Console.WriteLine($"Email: {googleUser.Email}");
            Console.WriteLine($"GoogleId (Sub): {googleUser.Sub}");
            Console.WriteLine($"Email Verified: {googleUser.EmailVerified}");

            if (!googleUser.EmailVerified)
            {
                Console.WriteLine("❌ Email not verified by Google");
                return new GoogleAuthResult
                {
                    Success = false,
                    ErrorType = GoogleAuthErrorType.EmailNotVerifiedByGoogle,
                    Message = "Email chưa được xác thực bởi Google"
                };
            }

            var email = googleUser.Email;
            var googleId = googleUser.Sub;

            // Tìm user theo GoogleId
            Console.WriteLine($"Searching user by GoogleId: {googleId}");
            var existingUserByGoogleId = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);

            // TRƯỜNG HỢP 2: GoogleId đã tồn tại
            if (existingUserByGoogleId != null)
            {
                Console.WriteLine($"✅ Found existing user by GoogleId: {existingUserByGoogleId.UserName}, Role: {existingUserByGoogleId.Role?.RoleName}");
                var (accessToken, refreshToken) = await _authService.GenerateTokensAsync(existingUserByGoogleId.UserId);

                return new GoogleAuthResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Role = existingUserByGoogleId.Role?.RoleName, // ✅ THÊM ROLE
                    Message = "Đăng nhập thành công"
                };
            }

            // Tìm user theo Email
            Console.WriteLine($"Searching user by Email: {email}");
            var existingUserByEmail = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            // TRƯỜNG HỢP 2b: Email tồn tại VÀ đã verify
            if (existingUserByEmail != null && existingUserByEmail.EmailConfirmed)
            {
                Console.WriteLine($"✅ Found existing verified user by Email: {existingUserByEmail.UserName}, Role: {existingUserByEmail.Role?.RoleName}");
                Console.WriteLine("Linking GoogleId to existing account...");

                existingUserByEmail.GoogleId = googleId;
                existingUserByEmail.LoginProvider = "Google";
                await _context.SaveChangesAsync();

                var (accessToken, refreshToken) = await _authService.GenerateTokensAsync(existingUserByEmail.UserId);

                return new GoogleAuthResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Role = existingUserByEmail.Role?.RoleName, // ✅ THÊM ROLE
                    Message = "Đã liên kết tài khoản với Google"
                };
            }

            // Email tồn tại NHƯNG chưa verify
            if (existingUserByEmail != null && !existingUserByEmail.EmailConfirmed)
            {
                Console.WriteLine($"✅ Found unverified user by Email: {existingUserByEmail.UserName}, Role: {existingUserByEmail.Role?.RoleName}");
                Console.WriteLine("Auto-verifying email and linking GoogleId...");

                existingUserByEmail.EmailConfirmed = true;
                existingUserByEmail.EmailConfirmedAt = DateTime.UtcNow;
                existingUserByEmail.GoogleId = googleId;
                existingUserByEmail.LoginProvider = "Google";
                await _context.SaveChangesAsync();

                var (accessToken, refreshToken) = await _authService.GenerateTokensAsync(existingUserByEmail.UserId);

                return new GoogleAuthResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Role = existingUserByEmail.Role?.RoleName, // ✅ THÊM ROLE
                    Message = "Email đã được xác thực và liên kết với Google"
                };
            }

            // TRƯỜNG HỢP 1: Email chưa tồn tại - Tạo user mới
            Console.WriteLine("Creating new user...");
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (defaultRole == null)
            {
                Console.WriteLine("❌ Customer role not found in database!");
                return new GoogleAuthResult
                {
                    Success = false,
                    ErrorType = GoogleAuthErrorType.UnknownError,
                    Message = "Lỗi hệ thống: không tìm thấy role Customer"
                };
            }

            var username = GenerateUniqueUsername(email);
            Console.WriteLine($"Generated username: {username}");

            var newUser = new User
            {
                RoleId = defaultRole.RoleId,
                UserName = username,
                Email = email,
                PasswordHash = null,
                FullName = googleUser.Name,
                EmailConfirmed = true,
                EmailConfirmedAt = DateTime.UtcNow,
                GoogleId = googleId,
                LoginProvider = "Google",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ New user created with UserId: {newUser.UserId}, Role: {defaultRole.RoleName}");

            var (newAccessToken, newRefreshToken) = await _authService.GenerateTokensAsync(newUser.UserId);

            return new GoogleAuthResult
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Role = defaultRole.RoleName, // ✅ THÊM ROLE (user mới luôn là Customer)
                Message = "Tài khoản mới đã được tạo thành công"
            };
        }

        private string GenerateUniqueUsername(string email)
        {
            var baseUsername = email.Split('@')[0];
            var username = $"{baseUsername}_google";

            var count = _context.Users.Count(u => u.UserName.StartsWith(username));
            if (count > 0)
            {
                username = $"{username}{count + 1}";
            }

            return username;
        }
    }
}
