using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IClientService
    {
        Task<List<Client>> GetClients();
    }
}
