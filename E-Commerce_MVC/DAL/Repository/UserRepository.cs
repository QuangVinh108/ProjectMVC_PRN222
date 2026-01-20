using DAL.Entities;
using Repositories.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Repository
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
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

            if (user == null) return null;

            // TODO: Thay BCrypt.Net bằng logic hash password thực tế của bạn
            // if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            if (password == "password") // Demo
            {
                return user;
            }

            return null;
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
    }
}