using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure;

namespace MindCare.Application.DataAccess.DbContext
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;


        public DbConnectionFactory(IConfiguration configuration) => _configuration = configuration;

        public DbContext ObterContexto(string AppSettingsSection)
        {
            IConfigurationSection configuracaoSessao = _configuration.GetSection(AppSettingsSection);
            AppSettingsDbConfiguracao configSessao = new();
            configuracaoSessao.Bind(configSessao);

            IDbConnection dbConnection;
            ISQLCommands sqlCommands;
            ISQLQuerys sqlQuerys;

            switch (configSessao.TipoBanco)
            {
                case EnumDbConnection.MySQL:
                    dbConnection = new MySqlConnection(configSessao.ConnectionString);
                    sqlQuerys = new MySQLQuerys();
                    sqlCommands = new MySQLCommands();
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new DbContext(dbConnection, sqlQuerys, sqlCommands);
        }
    }

    public enum EnumDbConnection
    {
        SQL,
        MySQL,
        Oracle,
        PhpMyAdmin
    }
}
