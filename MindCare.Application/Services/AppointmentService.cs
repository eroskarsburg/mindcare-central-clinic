using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IClientRepository _clientRepository;
        private readonly IPaymentRepository _paymentRepository;

        public AppointmentService(IAppointmentRepository repository, IClientRepository clientRepository, IPaymentRepository paymentRepository)
        {
            _repository = repository;
            _clientRepository = clientRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<List<Appointment>> GetAppointments()
        {
            List<Appointment> list = await _repository.Get();
            foreach (Appointment appointment in list)
            {
                appointment.Payment = await _paymentRepository.Get(appointmentId: appointment.Id);
                appointment.Client = await _clientRepository.Get(appointment.Client.Id);
            }
            if (list.Count > 0)
            {
                list.First().AllClients = list.First().AllClients = await _clientRepository.Get();
            }

            return list;
        }

        public async Task InsertAppointment(Appointment appoint)
        {
            await _repository.Insert(appoint);

            var newAppoint = await _repository.GetLast();
            appoint.Payment.IdAppointment = newAppoint.Id;

            await _paymentRepository.Insert(appoint.Payment);
        }

        public async Task UpdateAppointment(Appointment appoint)
        {
            await _repository.Update(appoint);

            await _paymentRepository.Update(appoint.Payment);
        }

        public async Task DeleteAppointment(int appointId, int paymentId)
        {
            var payment = await _paymentRepository.Get(appointmentId: appointId);
            await _paymentRepository.Delete(payment.Id);

            await _repository.Delete(appointId);
        }
    }
}
