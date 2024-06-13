using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;

        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<List<Client>> GetClients()
        {
            return await _clientRepository.Get();
        }

        public async Task InsertClient(Client client)
        {
            await _clientRepository.Insert(client);
        }

        public Task UpdateClient(Client client)
        {
            throw new NotImplementedException();
        }

        public Task DeleteClient(int clientId)
        {
            throw new NotImplementedException();
        }
    }
}
