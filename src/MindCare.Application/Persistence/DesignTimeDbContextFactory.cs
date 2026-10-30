using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MindCare.Application.Persistence;

/// <summary>
/// Usado apenas pelas ferramentas do EF (<c>dotnet ef migrations add</c>).
/// Lê a string de conexão da variável <c>ConnectionStrings__MindCare</c>, se existir.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MindCareDbContext>
{
    public MindCareDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MindCare")
            ?? "Server=localhost;Port=3306;Database=mindcare_db;User ID=mindcare;Password=mindcare;";

        var options = new DbContextOptionsBuilder<MindCareDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new MindCareDbContext(options);
    }
}
