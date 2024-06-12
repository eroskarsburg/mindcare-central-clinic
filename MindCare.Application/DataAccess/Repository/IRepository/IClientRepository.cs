using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IClientRepository
    {
        Task<List<Client>> Get();
    }
}
