using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepository
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
        Task AddUserAsync(User user);
        Task<User> GetUserByUserName(string username);
        User GetUserByEmail(string email);
        User GetUserByCccd(string cccdNumber);

        // ===== FOR AUTHSERVICE =====

        Task<User?> GetByIdWithRoleAsync(int userId);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task SaveChangesAsync();

        // ===== FOR EMAILVERIFICATIONSERVICE =====
        Task<User?> GetByIdAsync(int userId);
        Task<User?> FindVerifiedUserByEmailExcludingUserIdAsync(string email, int excludeUserId);
        Task<User?> FindGoogleUserByEmailExcludingUserIdAsync(string email, int excludeUserId);

        //====== DASHBOARD =====
        Task<int> GetTotalUserCountAsync();
        Task<int> GetNewUsersCountThisMonthAsync();
        Task<int> GetNewUsersCountLastMonthAsync();
        Task<List<User>> GetUsersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<int, int>> GetUserGrowthByMonthAsync(int months = 6);
        //===== ForGoogle ===== 
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task<User?> GetByEmailWithRoleAsync(string email);
        Task<int> CountUsersWithUsernameStartingWithAsync(string usernamePrefix);
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);



    }
}
