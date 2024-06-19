using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetPayments();
        Task InsertPayment(Payment payment);
        Task UpdatePayment(Payment payment);
        Task DeletePayment(int paymentId);
    }
}
