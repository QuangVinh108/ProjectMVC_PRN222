using BLL.DTOs;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.IService
{
    public interface IUserService
    {
        IEnumerable<User> GetAllUsers();
        User GetUserById(int id);
        void DeleteUser(int id);
        void CreateUser(CreateUserViewModel model);
        void UpdateUser(EditUserViewModel model);
        Task CreateUserAsync(CreateUserViewModel model);
        Task<User> GetUserByUserName(string username);
    }
}
