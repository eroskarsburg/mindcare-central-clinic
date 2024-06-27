using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IAppointmentService
    {
        Task<List<Appointment>> GetAppointments();
        Task InsertAppointment(Appointment appoint);
        Task UpdateAppointment(Appointment appoint);
        Task DeleteAppointment(int appointId, int paymentId);
    }
}
