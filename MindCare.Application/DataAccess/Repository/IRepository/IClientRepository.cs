using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IClientRepository
    {
        Task<List<Client>> Get();
        Task<Client> Get(int id);
        Task<Client> GetClientFromAppointment(int appointId);
        Task Insert(Client client);
        Task Update(Client client);
        Task Delete(int id);
    }
}
