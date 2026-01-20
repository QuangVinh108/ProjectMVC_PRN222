using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.IRepository
{
    public interface IUserRepository
    {
        User? GetByUserName(string username);
        Task<User?> AuthenticateAsync(string username, string password);
        IEnumerable<User> GetAllUsers();
        void AddUser(User user);
        void UpdateUser(User user);
        User GetUserById(int id); 
        void DeleteUser(int id);
    }
}
