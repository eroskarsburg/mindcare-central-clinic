using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;

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
            List<Client> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM clients";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    Client client = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Age = int.TryParse(_dbContext.Reader["age"].ToString(), out int age) ? age : 0
                    };
                    list.Add(client);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Client> Get(int id)
        {
            Client client = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM clients WHERE id_client={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    client = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Age = int.TryParse(_dbContext.Reader["age"].ToString(), out int age) ? age : 0
                    };
                }

                return await Task.FromResult(client);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Client> GetClientFromAppointment(int appointId)
        {
            Client client = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM mindcare_db.appointments a " +
                    $"INNER JOIN mindcare_db.clients c ON c.id_client = a.id_client WHERE a.id_appointment={appointId}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    client = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Age = int.TryParse(_dbContext.Reader["age"].ToString(), out int age) ? age : 0
                    };
                }

                return await Task.FromResult(client);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(Client client)
        {
            try
            {
                _dbContext.Query = "INSERT INTO clients (name, gender, cpf, age) " +
                $"VALUES('{client.Name}','{client.Gender}','{client.Cpf}', {client.Age})";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(Client client)
        {
            try
            {
                _dbContext.Query = $"UPDATE clients SET name='{client.Name}', gender='{client.Gender}', cpf='{client.Cpf}', age={client.Age} WHERE id_client={client.Id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }
        
        public async Task Delete(int id)
        {
            try
            {
                _dbContext.Query = $"DELETE FROM clients WHERE id_client={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }
    }
}
