using DAL.Entities;
using DAL.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
namespace DAL.Repository
{
    public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
    {
        private readonly ShopDbContext _context;

        public EmailVerificationTokenRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<EmailVerificationToken?> GetByTokenAsync(string token)
        {
            return await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(vt => vt.Token == token);
        }

        public async Task<EmailVerificationToken?> GetByTokenWithUserAsync(string token)
        {
            return await _context.EmailVerificationTokens
                .Include(vt => vt.User)
                .FirstOrDefaultAsync(vt => vt.Token == token);
        }

        public async Task AddAsync(EmailVerificationToken token)
        {
            await _context.EmailVerificationTokens.AddAsync(token);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
