using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository
{
    public class ProfessionalRepository : IProfessionalRepository
    {
        private readonly DbContextBase _dbContext;
        private readonly IUserRepository _userRepository;

        public ProfessionalRepository(DbContextBase dbContext, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
        }

        public async Task<List<Professional>> Get()
        {
            List<Professional> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM professionals";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    Professional professional = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_professional"].ToString(), out int idprof) ? idprof : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Speciality = _dbContext.Reader["speciality"].ToString() ?? string.Empty
                    };
                    list.Add(professional);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Professional> Get(int userId = 0, int profId = 0)
        {
            Professional professional = new();
            try
            {
                string condition = userId == 0 ? $"id_professional={profId}" : $"id_user_prof={userId}";
                _dbContext.Query = $"SELECT * FROM professionals WHERE {condition}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    professional = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_professional"].ToString(), out int idprof) ? idprof : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Speciality = _dbContext.Reader["speciality"].ToString() ?? string.Empty
                    };

                    if (userId == 0)
                    {
                        professional.User = new()
                        {
                            Id = int.TryParse(_dbContext.Reader["id_user_prof"].ToString(), out int iduser) ? iduser : 0,
                        };
                    }
                }

                return await Task.FromResult(professional);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(Professional professional)
        {
            try
            {
                _dbContext.Query = "INSERT INTO professionals (id_user_prof, name, gender, cpf, speciality) " +
                $"VALUES({professional.User.Id},'{professional.Name}','{professional.Gender}','{professional.Cpf}', '{professional.Speciality}')";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(Professional professional)
        {
            try
            {
                _dbContext.Query = $"UPDATE professionals SET name='{professional.Name}', cpf='{professional.Cpf}'" +
                    $", gender='{professional.Gender}', speciality='{professional.Speciality}' WHERE id_professional={professional.Id}";
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
                _dbContext.Query = $"DELETE FROM professionals WHERE id_professional={id}";
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
