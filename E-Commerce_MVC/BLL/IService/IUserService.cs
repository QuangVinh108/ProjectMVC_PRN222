using BLL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IService
{
    public interface IUserService
    {
        IEnumerable<User> GetAllUsers();
        User GetUserById(int id);
        void DeleteUser(int id);
        void CreateUser(CreateUserViewModel model);
        void UpdateUser(EditUserViewModel model);
        void UpdateUser(User user);
        Task CreateUserAsync(CreateUserViewModel model);
        Task<User> GetUserByUserName(string username);
        void UpdateProfile(int userId, UpdateProfileViewModel model);
        Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
