using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.Entities;
using System.Data.Common;

namespace MindCare.Application.DataAccess.Repository
{
    public class PatientRepository
    {
        private readonly IDbContext _dbContext;
        private readonly DbConnection _connection;

        public PatientRepository(IDbContext dbContext)
        {
            _dbContext = dbContext.Initialize();
        }

        public async IEnumerable<Patient> Select(Patient obj)
        {
            var newObj = 
            return await 
        }
    }
}
