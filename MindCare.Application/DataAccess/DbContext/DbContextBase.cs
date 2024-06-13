using Microsoft.Extensions.Configuration;
using MindCare.Application.Constants;
using MySql.Data.MySqlClient;

namespace MindCare.Application.DataAccess.DbContext
{
    public class DbContextBase : IDbContextBase
    {
        public string Query;
        public MySqlConnection Connection;
        public MySqlCommand Command;
        public MySqlDataReader Reader { get; set; }

        private readonly IConfiguration _configuration;

        public DbContextBase(IConfiguration configuration)
        {
            _configuration = configuration;
            Connection = new MySqlConnection(_configuration.GetConnectionString(AppSettingsConstants.MYSQL));
        }

        public void ExecuteQuery()
        {
            Command = new MySqlCommand(Query, Connection);
        }

        public void ExecuteNonQuery()
        {
            Command.ExecuteNonQuery();
        }

        public void ExecuteReader()
        {
            Reader = Command.ExecuteReader();
        }
    }
}
