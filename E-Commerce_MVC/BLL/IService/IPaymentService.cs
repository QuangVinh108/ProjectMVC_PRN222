using BLL.DTOs;
using Microsoft.AspNetCore.Http;

namespace BLL.IService
{
    public interface IPaymentService
    {
        string CreateVnPayUrl(PaymentDto payment, HttpContext context);
        bool HandleVnPayReturn(IQueryCollection query, out int orderId);
    }
}
