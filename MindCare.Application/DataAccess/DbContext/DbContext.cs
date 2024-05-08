using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Data;

namespace MindCare.Application.DataAccess.DbContext
{
    public class DbContext
    {
        public IDbConnection connection;
        public ISQLQuerys sqlQuery;
        public ISQLCommands sqlCommand;
        public DbContext(IDbConnection connection, ISQLQuerys siapQuery, ISQLCommands siapCommand)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(DbContext.connection));
            this.sqlQuery = siapQuery ?? throw new ArgumentNullException(nameof(sqlQuery));
            this.sqlCommand = siapCommand ?? throw new ArgumentNullException(nameof(sqlCommand));
        }
    }
}
