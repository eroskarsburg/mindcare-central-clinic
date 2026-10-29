using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MindCare.Application.Persistence;

namespace MindCare.Application.Tests;

/// <summary>Banco SQLite em memória, isolado por teste, com o mesmo modelo do EF usado em produção.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public MindCareDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MindCareDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>Relógio fixo para tornar os testes determinísticos.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
