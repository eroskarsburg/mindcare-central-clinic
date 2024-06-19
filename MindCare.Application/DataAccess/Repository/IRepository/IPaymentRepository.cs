using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> Get();
        Task<Payment> Get(int id);
        Task Insert(Payment payment);
        Task Update(Payment payment);
        Task Delete(int id);
    }
}
