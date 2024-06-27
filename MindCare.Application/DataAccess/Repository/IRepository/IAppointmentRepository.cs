using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IAppointmentRepository
    {
        Task<List<Appointment>> Get();
        Task<Appointment> Get(int id);
        Task<Appointment> GetLast();
        Task Insert(Appointment appoint);
        Task Update(Appointment appoint);
        Task Delete(int id);
    }
}
