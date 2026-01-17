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
    }
}