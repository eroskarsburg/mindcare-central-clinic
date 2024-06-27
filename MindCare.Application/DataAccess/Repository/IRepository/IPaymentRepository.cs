using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> Get();
        Task<Payment> Get(int paymentId = 0, int appointmentId = 0);
        Task Insert(Payment payment);
        Task Update(Payment payment);
        Task Delete(int id);
    }
}
