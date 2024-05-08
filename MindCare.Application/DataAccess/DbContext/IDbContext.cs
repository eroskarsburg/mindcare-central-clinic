using Microsoft.Extensions.Configuration;

namespace MindCare.Application.DataAccess.DbContext
{
    public interface IDbContext
    {
        public string Initialize(DbConnType conn);
    }
}
