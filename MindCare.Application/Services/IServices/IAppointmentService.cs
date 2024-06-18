using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IAppointmentService
    {
        Task<List<Appointment>> GetAppointments();
        Task InsertAppointment(Appointment client);
        Task UpdateAppointment(Appointment client);
        Task DeleteAppointment(int clientId);
    }
}
