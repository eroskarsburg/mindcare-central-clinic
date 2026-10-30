using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public sealed class LgpdTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));
    private readonly SystemActor _admin = new() { UserId = 1, Username = "admin", IpAddress = "10.0.0.1", AccessLevel = AccessLevel.Administrador };
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Criacao_e_alteracao_sao_auditadas_sem_valores()
    {
        using var db = _database.CreateContext(_admin);
        var service = Clients(db);

        var id = await service.CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: true, Ct);
        await service.UpdateAsync(new Client { Id = id, Name = "Ana Souza", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: true, Ct);

        var logs = await db.AuditLogs.AsNoTracking().OrderBy(a => a.Id).ToListAsync(Ct);
        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Equal("admin", l.Username));
        Assert.Equal((AuditActions.Created, id.ToString()), (logs[0].Action, logs[0].EntityId));
        Assert.Equal(AuditActions.Updated, logs[1].Action);
        Assert.Equal("Campos: Name", logs[1].Details);
        Assert.DoesNotContain("Souza", logs[1].Details!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Consentimento_registra_data_e_pode_ser_revogado()
    {
        using var db = _database.CreateContext();
        var service = Clients(db);
        var id = await service.CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: true, Ct);
        Assert.Equal(_clock.GetLocalNow().DateTime, (await db.Clients.FindAsync([id], Ct))!.ConsentAt);

        await service.UpdateAsync(new Client { Id = id, Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct);
        Assert.Null((await db.Clients.AsNoTracking().SingleAsync(Ct)).ConsentAt);
    }

    [Fact]
    public async Task Exportacao_inclui_consultas_e_e_auditada()
    {
        using var db = _database.CreateContext(_admin);
        var id = await Clients(db).CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: true, Ct);
        await new AppointmentService(db, _clock).CreateAsync(
            new AppointmentInput(id, AppointmentModality.Presencial, new DateTime(2026, 5, 6, 14, 0, 0), "Primeira", 150, 0), Ct);

        var export = await Clients(db).ExportAsync(id, Ct);

        Assert.Single(export.Appointments);
        Assert.Contains(db.AuditLogs, a => a.Action == AuditActions.Exported && a.EntityId == id.ToString());
    }

    [Fact]
    public async Task Anonimizacao_remove_dados_pessoais_e_bloqueia_edicao()
    {
        using var db = _database.CreateContext(_admin);
        var id = await Clients(db).CreateAsync(new Client { Name = "Ana Moura", Cpf = "52998224725", Gender = "Feminino", Age = 30 }, consentGiven: true, Ct);
        await new AppointmentService(db, _clock).CreateAsync(
            new AppointmentInput(id, AppointmentModality.Presencial, new DateTime(2026, 5, 6, 14, 0, 0), "Relato sensível", 150, 150), Ct);

        await Clients(db).AnonymizeAsync(id, Ct);

        var client = await db.Clients.AsNoTracking().Include(c => c.Appointments).ThenInclude(a => a.Payment).SingleAsync(Ct);
        Assert.True(client.IsAnonymized);
        Assert.DoesNotContain("Ana", client.Name, StringComparison.Ordinal);
        Assert.False(Cpf.IsValid(client.Cpf));
        Assert.Null(client.Age);
        Assert.Null(client.Appointments.Single().Observation);
        Assert.Equal(150, client.Appointments.Single().Payment!.PaidPrice); // histórico financeiro preservado

        await Assert.ThrowsAsync<BusinessException>(() =>
            Clients(db).UpdateAsync(new Client { Id = id, Name = "X", Cpf = "52998224725", Gender = "Outro" }, false, Ct));
    }

    private ClientService Clients(MindCareDbContext db) => new(db, new AuditService(db, _admin, _clock), _clock);

    public void Dispose() => _database.Dispose();
}
