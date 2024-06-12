using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using System.Collections.Generic;

namespace MindCare.Application.DataAccess.Repository
{
    public class ClientRepository : IClientRepository
    {
        private readonly DbContextBase _dbContext;

        public ClientRepository(DbContextBase dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Client>> Get()
        {
            Client client = new();
            List<Client> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM clients";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    client.Name = _dbContext.Reader["name"].ToString() ?? string.Empty;
                    client.Birth = DateOnly.TryParse(_dbContext.Reader["birth"].ToString(), out DateOnly dateOnly) ? dateOnly : DateOnly.MinValue;
                    client.Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty;
                    list.Add(client);
                }
                await _dbContext.Connection.CloseAsync();

                return await Task.FromResult(list);
            }
            catch (Exception) { throw; }
        }
    }
}
