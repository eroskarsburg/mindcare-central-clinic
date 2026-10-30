using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public sealed class ServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PasswordService _passwords = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Cliente_com_cpf_duplicado_e_rejeitado()
    {
        using var db = _database.CreateContext();
        var service = Clients(db);
        await service.CreateAsync(new Client { Name = "Ana", Cpf = "529.982.247-25", Gender = "Feminino" }, consentGiven: false, Ct);

        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(new Client { Name = "Outra", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct));

        Assert.Contains("CPF", error.Message, StringComparison.Ordinal);
        Assert.Equal("52998224725", (await db.Clients.SingleAsync(Ct)).Cpf);
    }

    [Fact]
    public async Task Cliente_com_agendamento_nao_pode_ser_excluido()
    {
        using var db = _database.CreateContext();
        var clientId = await Clients(db).CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct);
        await new AppointmentService(db, _clock).CreateAsync(
            new AppointmentInput(clientId, AppointmentModality.Presencial, new DateTime(2026, 5, 6, 14, 0, 0), null, 150, 0), Ct);

        await Assert.ThrowsAsync<BusinessException>(() => Clients(db).DeleteAsync(clientId, Ct));
    }

    [Fact]
    public async Task Agendamento_cria_pagamento_com_status_calculado()
    {
        using var db = _database.CreateContext();
        var clientId = await Clients(db).CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct);
        var service = new AppointmentService(db, _clock);

        var result = await service.CreateAsync(
            new AppointmentInput(clientId, AppointmentModality.Teleconsulta, new DateTime(2026, 5, 6, 14, 0, 0), "  primeira sessão ", 180, 60), Ct);

        Assert.Equal(1, result.Affected);
        var appointment = await db.Appointments.Include(a => a.Payment).AsNoTracking().SingleAsync(Ct);
        Assert.Equal("primeira sessão", appointment.Observation);
        Assert.Equal(PaymentStatus.Parcial, appointment.Payment!.Status);
        Assert.Equal(new DateTime(2026, 5, 4), appointment.Payment.PaidDate);
        Assert.StartsWith("https://meet.jit.si/mindcare-", appointment.MeetingUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Agendamento_com_valor_pago_maior_que_o_valor_e_rejeitado()
    {
        using var db = _database.CreateContext();
        var clientId = await Clients(db).CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct);

        await Assert.ThrowsAsync<BusinessException>(() => new AppointmentService(db, _clock).CreateAsync(
            new AppointmentInput(clientId, AppointmentModality.Presencial, new DateTime(2026, 5, 6, 14, 0, 0), null, 100, 150), Ct));
    }

    [Fact]
    public async Task Excluir_agendamento_remove_pagamento_em_cascata()
    {
        using var db = _database.CreateContext();
        var clientId = await Clients(db).CreateAsync(new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" }, consentGiven: false, Ct);
        var service = new AppointmentService(db, _clock);
        await service.CreateAsync(
            new AppointmentInput(clientId, AppointmentModality.Presencial, new DateTime(2026, 5, 6, 14, 0, 0), null, 100, 0), Ct);

        await service.DeleteAsync((await db.Appointments.SingleAsync(Ct)).Id, Ct);

        Assert.False(await db.Payments.AnyAsync(Ct));
    }

    [Fact]
    public async Task Profissional_recebe_usuario_com_senha_temporaria_aleatoria()
    {
        using var db = _database.CreateContext();
        var created = await new ProfessionalService(db, _passwords).CreateAsync(
            new Professional { Name = "Letícia Kasper", Cpf = "153.509.460-56", Gender = "Feminino", Speciality = "Psicoterapia" }, Ct);

        Assert.Equal("15350946056", created.Username);
        Assert.Equal(12, created.InitialPassword.Length);
        Assert.DoesNotContain("15350946056", created.InitialPassword, StringComparison.Ordinal);

        var user = await db.Users.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(AccessLevel.Profissional, user.AccessLevel);
        Assert.True(user.MustChangePassword);
        Assert.True(_passwords.Verify(user, created.InitialPassword, out _));
    }

    [Fact]
    public async Task Excluir_profissional_remove_o_usuario()
    {
        using var db = _database.CreateContext();
        var service = new ProfessionalService(db, _passwords);
        var created = await service.CreateAsync(
            new Professional { Name = "Letícia", Cpf = "15350946056", Gender = "Feminino", Speciality = "Psicoterapia" }, Ct);

        await service.DeleteAsync(created.Id, Ct);

        Assert.False(await db.Users.AnyAsync(Ct));
        Assert.False(await db.Professionals.AnyAsync(Ct));
    }

    [Fact]
    public async Task Usuario_nao_pode_se_excluir_nem_remover_ultimo_admin()
    {
        using var db = _database.CreateContext();
        var service = new UserService(db, _passwords);
        var adminId = await service.CreateAsync("admin", "senha-forte-1", AccessLevel.Administrador, Ct);
        var otherId = await service.CreateAsync("outro", "senha-forte-2", AccessLevel.Profissional, Ct);

        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(adminId, adminId, Ct));
        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(adminId, otherId, Ct));
        await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(adminId, "admin", null, AccessLevel.Profissional, adminId, Ct));
    }

    [Fact]
    public async Task Atualizar_usuario_sem_senha_mantem_a_atual()
    {
        using var db = _database.CreateContext();
        var service = new UserService(db, _passwords);
        var id = await service.CreateAsync("ana", "senha-original", AccessLevel.Profissional, Ct);

        await service.UpdateAsync(id, "ana.souza", null, AccessLevel.Profissional, currentUserId: 999, Ct);

        var user = await db.Users.AsNoTracking().SingleAsync(Ct);
        Assert.Equal("ana.souza", user.Username);
        Assert.True(_passwords.Verify(user, "senha-original", out _));
    }

    private ClientService Clients(MindCareDbContext db) =>
        new(db, new AuditService(db, new SystemActor(), _clock), _clock);

    public void Dispose() => _database.Dispose();
}
