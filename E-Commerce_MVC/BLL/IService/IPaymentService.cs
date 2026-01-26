using BLL.DTOs;
using Microsoft.AspNetCore.Http;

namespace BLL.IService
{
    public interface IPaymentService
    {
        string CreateVnPayUrl(PaymentDto payment, HttpContext context);
        Task<(bool Success, string Message, int OrderId)> ProcessVnPayReturnAsync(IQueryCollection query);
        Task CreatePendingPaymentAsync(int orderId, decimal amount);

    }
}
