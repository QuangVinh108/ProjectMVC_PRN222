using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepository
{
    public interface IPaymentRepository
    {
        Task<int> UpdateStatusAsync(int orderId, string status, DateTime? padiAt);
    }
}
