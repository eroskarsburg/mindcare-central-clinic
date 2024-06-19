using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class PaymentService : IPaymentService
    {
        public readonly IPaymentRepository _repository;

        public PaymentService(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Payment>> GetPayments()
        {
            return await _repository.Get();
        }

        public async Task InsertPayment(Payment payment)
        {
            await _repository.Insert(payment);
        }

        public async Task UpdatePayment(Payment payment)
        {
            await _repository.Update(payment);
        }

        public async Task DeletePayment(int paymentId)
        {
            await _repository.Delete(paymentId);
        }
    }
}
