using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;
using DAL.IRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ShopDbContext _context;
        public PaymentRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<int> UpdateStatusAsync(int orderId, string status, DateTime? paidAt)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null) return 0;

            payment.Status = status;
            payment.PaidAt = paidAt;

            return await _context.SaveChangesAsync();
        }

    }
}
