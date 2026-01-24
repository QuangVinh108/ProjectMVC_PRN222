using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepository
{
    public interface IEmailVerificationTokenRepository
    {
        Task<EmailVerificationToken?> GetByTokenAsync(string token);
        Task<EmailVerificationToken?> GetByTokenWithUserAsync(string token);
        Task AddAsync(EmailVerificationToken token);
        Task SaveChangesAsync();
    }
}
