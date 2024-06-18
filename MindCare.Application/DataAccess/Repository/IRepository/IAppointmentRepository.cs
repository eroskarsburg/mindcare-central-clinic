using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> Get();
        Task Insert(Appointment client);
        Task Update(Appointment client);
        Task Delete(int id);
    }
}
