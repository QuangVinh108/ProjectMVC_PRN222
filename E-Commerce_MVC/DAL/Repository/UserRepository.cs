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
    }
}