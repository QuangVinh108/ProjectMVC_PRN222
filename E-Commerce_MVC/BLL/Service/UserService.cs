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
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);            

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

        public void UpdateUser(User user)
        {
            // Có thể thêm validate logic chung nếu cần
            _userRepository.UpdateUser(user);
        }

        public async Task CreateUserAsync(CreateUserViewModel model)
        {
            var newUser = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
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

        public User GetUserByCccd(string cccdNumber)
        {
            return _userRepository.GetUserByCccd(cccdNumber);
        }

        public void UpdateProfile(int userId, UpdateProfileViewModel model)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null) 
                throw new Exception("Không tìm thấy người dùng.");

            if (model.Email != user.Email)
            {
                var existingUser = _userRepository.GetUserByEmail(model.Email);
                if (existingUser != null)
                {
                    throw new Exception("Email này đã được sử dụng bởi tài khoản khác!");
                }
                user.Email = model.Email; 
            }

            if (!user.IsIdentityVerified)
            {
                user.FullName = model.FullName;
            }
           
            user.Phone = model.Phone;
            user.Address = model.Address;

            _userRepository.UpdateUser(user);
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // 1. Lấy user từ DB
            var user = _userRepository.GetUserById(userId);
            if (user == null) return (false, "Tài khoản không tồn tại.");

            // 2. Nếu là tài khoản Google (không có password), chặn đổi pass
            if (string.IsNullOrEmpty(user.PasswordHash))
                return (false, "Tài khoản đăng nhập bằng Google không thể đổi mật khẩu.");

            // 3. Kiểm tra mật khẩu cũ (Dùng BCrypt để verify)
            // Lưu ý: Cần cài gói NuGet: BCrypt.Net-Next
            bool isCorrect = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);

            if (!isCorrect)
            {
                return (false, "Mật khẩu hiện tại không chính xác.");
            }

            // 4. Mã hóa mật khẩu mới
            string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // 5. Cập nhật và lưu
            user.PasswordHash = newHash;
            _userRepository.UpdateUser(user);

            return (true, "Đổi mật khẩu thành công.");
        }
    }
}
