﻿using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IClientService
    {
        Task<List<Client>> GetClients();
        Task InsertClient(Client client);
        Task UpdateClient(Client client);
        Task DeleteClient(int clientId);
    }
}
