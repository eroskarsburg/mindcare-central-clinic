using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IClientRepository
    {
        Task<List<Client>> Get();
        Task Insert(Client client);
        Task Update(Client client);
        Task Delete(int id);
    }
}
