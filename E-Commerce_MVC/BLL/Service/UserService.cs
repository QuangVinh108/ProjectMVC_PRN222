using BLL.DTOs;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.EntityFrameworkCore;
using Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _userRepository.GetAllUsers();
        }

        public User GetUserById(int id)
        {
            return _userRepository.GetUserById(id);
        }

        public void DeleteUser(int id)
        {
            // Ví dụ: Kiểm tra logic nghiệp vụ trước khi xóa
            // if (id == 1) throw new Exception("Không thể xóa Admin tối cao");

            _userRepository.DeleteUser(id);
        }

        public void CreateUser(CreateUserViewModel model)
        {
            // 1. Hash mật khẩu (Giả lập hash đơn giản, thực tế nên dùng BCrypt)
            // var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            var passwordHash = model.Password; // Tạm thời lưu thô (Lưu ý: Cần sửa lại để bảo mật)

            // 2. Map từ ViewModel sang Entity
            var newUser = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = passwordHash,
                FullName = model.FullName,
                Phone = model.Phone,
                Address = model.Address,
                RoleId = model.RoleId,

                // Các giá trị mặc định
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _userRepository.AddUser(newUser);
        }

        public void UpdateUser(EditUserViewModel model)
        {
            // 1. Lấy thông tin cũ từ database
            var user = _userRepository.GetUserById(model.UserId);

            if (user == null)
            {
                throw new Exception("Không tìm thấy người dùng!");
            }

            // 2. Chỉ cập nhật những trường được phép sửa
            user.UserName = model.UserName; // Có thể bỏ dòng này nếu không cho sửa username
            user.Email = model.Email;
            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Address = model.Address;
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;

            // 3. Gọi Repo để lưu
            _userRepository.UpdateUser(user);
        }

        public async Task CreateUserAsync(CreateUserViewModel model)
        {
            var newUser = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                //PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                PasswordHash = model.Password,
                FullName = model.FullName,
                Phone = model.Phone,
                Address = model.Address,
                RoleId = model.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddUserAsync(newUser);
        }

        public async Task<User> GetUserByUserName(string username)
        {
            return await _userRepository.GetUserByUserName(username);
        }
    }
}
