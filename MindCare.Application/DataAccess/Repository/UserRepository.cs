using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using System.Globalization;

namespace MindCare.Application.DataAccess.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DbContextBase _dbContext;

        public UserRepository(DbContextBase dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<User>> Get()
        {
            List<User> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM users";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = string.IsNullOrEmpty(_dbContext.Reader["last_activity"].ToString()) ? "1/1/0001 12:00:00 AM" : _dbContext.Reader["last_activity"].ToString();

                    User user = new User();
                    user.Id = int.TryParse(_dbContext.Reader["id_user"].ToString(), out int idprof) ? idprof : 0;
                        user.Username = _dbContext.Reader["username"].ToString() ?? string.Empty;
                        user.Password = _dbContext.Reader["password"].ToString() ?? string.Empty;
                        user.AccessLevel = (EnumAccessLevel)Enum.Parse(typeof(EnumAccessLevel), _dbContext.Reader["id_access_level"].ToString() ?? string.Empty);
                        user.LastActivity = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                    list.Add(user);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public Task<User> Get(int id)
        {
            throw new NotImplementedException();
        }

        public Task Insert(User user)
        {
            throw new NotImplementedException();
        }

        public Task Update(User user)
        {
            throw new NotImplementedException();
        }
        
        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
