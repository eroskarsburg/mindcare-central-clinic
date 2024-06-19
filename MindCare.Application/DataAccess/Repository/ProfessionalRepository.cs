using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository
{
    public class ProfessionalRepository : IProfessionalRepository
    {
        private readonly DbContextBase _dbContext;

        public ProfessionalRepository(DbContextBase dbContext)
        {
            _dbContext = dbContext;
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
                        IdUser = int.TryParse(_dbContext.Reader["id_user_prof"].ToString(), out int iduser) ? iduser : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Age = int.TryParse(_dbContext.Reader["age"].ToString(), out int age) ? age : 0
                    };
                    list.Add(professional);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public Task<Professional> Get(int id)
        {
            throw new NotImplementedException();
        }

        public Task Insert(Professional professional)
        {
            throw new NotImplementedException();
        }

        public Task Update(Professional professional)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}
