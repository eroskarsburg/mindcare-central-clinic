using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using Org.BouncyCastle.Asn1.X509;
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

        public async Task<User> Get(User user)
        {
            User newUser = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM users WHERE username='{user.Username}' AND password='{user.Password}'";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = string.IsNullOrEmpty(_dbContext.Reader["last_activity"].ToString()) ? "1/1/0001 12:00:00 AM" : _dbContext.Reader["last_activity"].ToString();

                    newUser.Id = int.TryParse(_dbContext.Reader["id_user"].ToString(), out int idprof) ? idprof : 0;
                    newUser.Username = _dbContext.Reader["username"].ToString() ?? string.Empty;
                    newUser.Password = _dbContext.Reader["password"].ToString() ?? string.Empty;
                    newUser.AccessLevel = (EnumAccessLevel)Enum.Parse(typeof(EnumAccessLevel), _dbContext.Reader["id_access_level"].ToString() ?? string.Empty);
                    newUser.LastActivity = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                }

                return await Task.FromResult(newUser);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<User> Get(int id = 0, string username = "")
        {
            User newUser = new();
            try
            {
                string condition = id == 0 ? $"username='{username}'" : $"id_user={id}";
                _dbContext.Query = $"SELECT * FROM users WHERE {condition}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = string.IsNullOrEmpty(_dbContext.Reader["last_activity"].ToString()) ? "1/1/0001 12:00:00 AM" : _dbContext.Reader["last_activity"].ToString();

                    newUser.Id = int.TryParse(_dbContext.Reader["id_user"].ToString(), out int idprof) ? idprof : 0;
                    newUser.Username = _dbContext.Reader["username"].ToString() ?? string.Empty;
                    newUser.Password = _dbContext.Reader["password"].ToString() ?? string.Empty;
                    newUser.AccessLevel = (EnumAccessLevel)Enum.Parse(typeof(EnumAccessLevel), _dbContext.Reader["id_access_level"].ToString() ?? string.Empty);
                    newUser.LastActivity = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                }

                return await Task.FromResult(newUser);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(User user)
        {
            try
            {
                _dbContext.Query = "INSERT INTO users (username, password, id_access_level, last_activity) " +
                $"VALUES('{user.Username}','{user.Password}','{(int)user.AccessLevel}', NOW())";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(User user)
        {
            try
            {
                _dbContext.Query = $"UPDATE users SET username='{user.Username}', password='{user.Password}'," +
                $"id_access_level={(int)user.AccessLevel} WHERE id_user={user.Id}";
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
                _dbContext.Query = $"DELETE FROM users WHERE id_user={id}";
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
