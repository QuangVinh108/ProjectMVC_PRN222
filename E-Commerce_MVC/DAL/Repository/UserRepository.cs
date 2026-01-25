using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DAL.IRepository;
using BCrypt;
namespace DAL.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ShopDbContext _context;

        public UserRepository(ShopDbContext context)
        {
            _context = context;
        }

        public User? GetByUserName(string username)
        {
            return _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserName == username && u.IsActive);
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            Console.WriteLine($"=== REPOSITORY AUTHENTICATE === Username: {username}");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null)
            {
                Console.WriteLine("❌ User not found");
                return null;
            }

            Console.WriteLine($"✅ User found, verifying password...");

            // ✅ VERIFY PASSWORD BẰNG BCRYPT
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isValidPassword)
            {
                Console.WriteLine("❌ Password verification failed");
                return null;
            }

            Console.WriteLine("✅ Password verified successfully");

            // Check active status
            if (!user.IsActive)
            {
                Console.WriteLine("❌ User is not active");
                return null;
            }

            return user;
        }


        // 1. Hàm lấy danh sách tất cả User
        public IEnumerable<User> GetAllUsers()
        {
            // Giả sử trong ShopDbContext bạn đặt tên bảng là Users
            return _context.Users.Include(u => u.Role).ToList();
        }

        // 2. Hàm lấy User theo ID
        public User GetUserById(int id)
        {
            // .Find() là cách nhanh nhất để tìm theo Khóa Chính (Primary Key)
            return _context.Users.Find(id);

            // Hoặc nếu muốn an toàn hơn có thể dùng:
            // return _context.Users.FirstOrDefault(u => u.UserId == id);
        }

        // 3. Hàm Xóa User
        public void DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                // --- Code cũ (Xóa cứng) ---
                // _context.Users.Remove(user); 

                // --- Code mới (Xóa mềm) ---
                user.IsActive = false; // Đổi trạng thái thành Inactive

                _context.Users.Update(user); // Đánh dấu là cập nhật
                _context.SaveChanges();      // Lưu vào database
            }
        }
        public void AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public Task<User> GetUserByUserName(string username)
        {
            return _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);
        }
        public User GetUserByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }
        public async Task<User?> GetByIdWithRoleAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> FindVerifiedUserByEmailExcludingUserIdAsync(string email, int excludeUserId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email
                                       && u.EmailConfirmed
                                       && u.UserId != excludeUserId);
        }

        public async Task<User?> FindGoogleUserByEmailExcludingUserIdAsync(string email, int excludeUserId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email
                                       && u.GoogleId != null
                                       && u.EmailConfirmed
                                       && u.UserId != excludeUserId);
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        // ====  DASHBOARD ==== 

        public async Task<int> GetTotalUserCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetNewUsersCountThisMonthAsync()
        {
            var firstDayThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await _context.Users
                .CountAsync(u => u.CreatedAt >= firstDayThisMonth);
        }

        public async Task<int> GetNewUsersCountLastMonthAsync()
        {
            var now = DateTime.Now;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            return await _context.Users
                .CountAsync(u => u.CreatedAt >= firstDayLastMonth && u.CreatedAt < firstDayThisMonth);
        }

        public async Task<List<User>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetUserGrowthByMonthAsync(int months = 6)
        {
            var startDate = DateTime.Now.AddMonths(-months);

            var users = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    MonthKey = g.Key.Year * 100 + g.Key.Month, // 202601
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.MonthKey, x => x.Count);

            return users;
        }
        /// <summary>
        /// Tìm user theo GoogleId (bao gồm cả Role)
        /// </summary>
        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        /// <summary>
        /// Tìm user theo Email kèm Role
        /// </summary>
        public async Task<User?> GetByEmailWithRoleAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Đếm số user có username bắt đầu bằng prefix (dùng để generate unique username)
        /// </summary>
        public async Task<int> CountUsersWithUsernameStartingWithAsync(string usernamePrefix)
        {
            return await _context.Users
                .CountAsync(u => u.UserName.StartsWith(usernamePrefix));
        }
        public async Task<User> CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Cập nhật user (async)
        /// </summary>
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }


    }
}