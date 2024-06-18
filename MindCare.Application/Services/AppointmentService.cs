using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IClientRepository _clientRepository;

        public AppointmentService(IAppointmentRepository repository, IClientRepository clientRepository)
        {
            _repository = repository;
            _clientRepository = clientRepository;
        }

        public async Task<List<Appointment>> GetAppointments()
        {
            List<Appointment> list = await _repository.Get();
            foreach (Appointment appointment in list)
            {
                appointment.Client = await _clientRepository.Get(appointment.Client.Id);
            }
            return list;
        }

        public Task InsertAppointment(Appointment client)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAppointment(Appointment client)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAppointment(int clientId)
        {
            throw new NotImplementedException();
        }
    }
}
