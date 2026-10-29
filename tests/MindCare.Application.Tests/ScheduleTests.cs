using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public sealed class ScheduleTests : IDisposable
{
    // Segunda-feira, 4 de maio de 2026, 10h.
    private static readonly DateTime Monday = new(2026, 5, 4);
    private readonly TestDatabase _database = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Bloqueia_conflito_do_profissional_e_do_paciente()
    {
        using var db = _database.CreateContext();
        var (ana, bia, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(14)), Ct);

        // Mesmo profissional, horário sobreposto (14:30 dentro de 14:00–14:50).
        var professional = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(Input(bia, leticia, Monday.AddDays(1).AddHours(14.5)), Ct));
        Assert.StartsWith("O profissional", professional.Message, StringComparison.Ordinal);

        // Mesmo paciente com outro profissional no mesmo horário.
        var marcos = await AddProfessionalAsync(db, "Marcos", "46873529010");
        var patient = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateAsync(Input(ana, marcos, Monday.AddDays(1).AddHours(14.25)), Ct));
        Assert.StartsWith("O paciente", patient.Message, StringComparison.Ordinal);

        // Encostado no fim (14:50) não é conflito.
        await service.CreateAsync(Input(bia, leticia, Monday.AddDays(1).AddHours(14).AddMinutes(50)), Ct);
        Assert.Equal(2, await db.Appointments.CountAsync(Ct));
    }

    [Fact]
    public async Task Consulta_cancelada_libera_o_horario()
    {
        using var db = _database.CreateContext();
        var (ana, bia, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(14)), Ct);
        var id = (await db.Appointments.SingleAsync(Ct)).Id;

        await service.SetStatusAsync(id, AppointmentStatus.Cancelada, "Imprevisto", ct: Ct);
        await service.CreateAsync(Input(bia, leticia, Monday.AddDays(1).AddHours(14)), Ct);

        var cancelled = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == id, Ct);
        Assert.Equal("Imprevisto", cancelled.CancellationReason);
    }

    [Fact]
    public async Task Serie_semanal_pula_bloqueios_e_avisa()
    {
        using var db = _database.CreateContext();
        var (ana, _, leticia) = await SeedAsync(db);
        db.ScheduleBlocks.Add(new ScheduleBlock { StartsAt = Monday.AddDays(15), EndsAt = Monday.AddDays(16), Reason = "Feriado" });
        await db.SaveChangesAsync(Ct);

        var result = await Appointments(db).CreateAsync(
            Input(ana, leticia, Monday.AddDays(1).AddHours(9)) with { Recurrence = RecurrenceFrequency.Semanal, Occurrences = 4 }, Ct);

        Assert.Equal(3, result.Affected);
        Assert.Contains(result.Warnings, w => w.Contains("19/05", StringComparison.Ordinal));
        var all = await db.Appointments.AsNoTracking().Include(a => a.Payment).OrderBy(a => a.ScheduledAt).ToListAsync(Ct);
        Assert.Single(all.Select(a => a.SeriesId).Distinct());
        Assert.Equal([5, 12, 26], all.Select(a => a.ScheduledAt.Day));
        Assert.Equal(60, all[0].Payment!.PaidPrice); // valor pago só na primeira sessão
        Assert.All(all.Skip(1), a => Assert.Equal(0, a.Payment!.PaidPrice));
    }

    [Fact]
    public async Task Consulta_avulsa_em_periodo_bloqueado_e_rejeitada()
    {
        using var db = _database.CreateContext();
        var (ana, _, leticia) = await SeedAsync(db);
        db.ScheduleBlocks.Add(new ScheduleBlock { ProfessionalId = leticia, StartsAt = Monday.AddDays(1), EndsAt = Monday.AddDays(8), Reason = "Férias" });
        await db.SaveChangesAsync(Ct);

        var error = await Assert.ThrowsAsync<BusinessException>(() =>
            Appointments(db).CreateAsync(Input(ana, leticia, Monday.AddDays(2).AddHours(9)), Ct));
        Assert.Contains("Férias", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Alterar_esta_e_as_proximas_desloca_a_serie()
    {
        using var db = _database.CreateContext();
        var (ana, _, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(9)) with { Recurrence = RecurrenceFrequency.Semanal, Occurrences = 4 }, Ct);
        var second = await db.Appointments.OrderBy(a => a.ScheduledAt).Skip(1).FirstAsync(Ct);

        // Muda da terça 9h para quarta 15h a partir da 2ª sessão.
        var result = await service.UpdateAsync(second.Id,
            Input(ana, leticia, second.ScheduledAt.AddDays(1).AddHours(6)) with { Price = 200 }, SeriesScope.EstaEProximas, Ct);

        Assert.Equal(3, result.Affected);
        var all = await db.Appointments.AsNoTracking().Include(a => a.Payment).OrderBy(a => a.ScheduledAt).ToListAsync(Ct);
        Assert.Equal(DayOfWeek.Tuesday, all[0].ScheduledAt.DayOfWeek);
        Assert.All(all.Skip(1), a => Assert.Equal((DayOfWeek.Wednesday, 15), (a.ScheduledAt.DayOfWeek, a.ScheduledAt.Hour)));
        Assert.Equal(150, all[0].Payment!.Price);
        Assert.All(all.Skip(1), a => Assert.Equal(200, a.Payment!.Price));
    }

    [Fact]
    public async Task Cancelar_serie_a_partir_de_uma_sessao()
    {
        using var db = _database.CreateContext();
        var (ana, _, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(9)) with { Recurrence = RecurrenceFrequency.Quinzenal, Occurrences = 4 }, Ct);
        var third = await db.Appointments.OrderBy(a => a.ScheduledAt).Skip(2).FirstAsync(Ct);

        var result = await service.SetStatusAsync(third.Id, AppointmentStatus.Cancelada, "Alta", SeriesScope.EstaEProximas, Ct);

        Assert.Equal(2, result.Affected);
        var statuses = await db.Appointments.AsNoTracking().OrderBy(a => a.ScheduledAt).Select(a => a.Status).ToListAsync(Ct);
        Assert.Equal([AppointmentStatus.Agendada, AppointmentStatus.Agendada, AppointmentStatus.Cancelada, AppointmentStatus.Cancelada], statuses);
    }

    [Fact]
    public async Task Presenca_e_falta_so_em_consultas_que_ja_comecaram()
    {
        using var db = _database.CreateContext();
        var (ana, bia, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddHours(8)), Ct);
        await service.CreateAsync(Input(bia, leticia, Monday.AddDays(1).AddHours(8)), Ct);
        var (past, future) = (await db.Appointments.OrderBy(a => a.ScheduledAt).FirstAsync(Ct), await db.Appointments.OrderBy(a => a.ScheduledAt).LastAsync(Ct));

        await service.SetStatusAsync(past.Id, AppointmentStatus.Faltou, null, ct: Ct);
        await Assert.ThrowsAsync<BusinessException>(() => service.SetStatusAsync(future.Id, AppointmentStatus.Realizada, null, ct: Ct));
    }

    [Fact]
    public async Task Remarcar_valida_conflito_e_desfaz_confirmacao()
    {
        using var db = _database.CreateContext();
        var (ana, bia, leticia) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(9)), Ct);
        await service.CreateAsync(Input(bia, leticia, Monday.AddDays(1).AddHours(11)), Ct);
        var first = await db.Appointments.OrderBy(a => a.ScheduledAt).FirstAsync(Ct);
        await service.SetStatusAsync(first.Id, AppointmentStatus.Confirmada, null, ct: Ct);

        await Assert.ThrowsAsync<BusinessException>(() => service.RescheduleAsync(first.Id, Monday.AddDays(1).AddHours(11.5), null, Ct));
        await service.RescheduleAsync(first.Id, Monday.AddDays(1).AddHours(13), 60, Ct);

        var moved = await db.Appointments.AsNoTracking().SingleAsync(a => a.Id == first.Id, Ct);
        Assert.Equal((13, 60, AppointmentStatus.Agendada), (moved.ScheduledAt.Hour, moved.DurationMinutes, moved.Status));
    }

    [Fact]
    public async Task Profissional_logado_ve_e_altera_apenas_a_propria_agenda()
    {
        using var db = _database.CreateContext();
        var (ana, bia, leticia) = await SeedAsync(db);
        var marcos = await AddProfessionalAsync(db, "Marcos", "46873529010");
        await Appointments(db).CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(9)), Ct);
        await Appointments(db).CreateAsync(Input(bia, marcos, Monday.AddDays(1).AddHours(9)), Ct);

        var marcosUser = await db.Professionals.Where(p => p.Id == marcos).Select(p => p.UserId).SingleAsync(Ct);
        var actor = new SystemActor { UserId = marcosUser, Username = "marcos", AccessLevel = AccessLevel.Profissional };
        var scoped = new AppointmentService(db, _clock, new AccessScope(db, actor));

        var visible = await scoped.ListAsync(ct: Ct);
        Assert.Equal([marcos], visible.Select(a => a.ProfessionalId!.Value));

        // Ao agendar, o profissional é sempre ele mesmo, mesmo que outro seja informado.
        await scoped.CreateAsync(Input(ana, leticia, Monday.AddDays(2).AddHours(9)), Ct);
        Assert.Equal(marcos, (await db.Appointments.OrderBy(a => a.Id).LastAsync(Ct)).ProfessionalId);

        var other = await db.Appointments.FirstAsync(a => a.ProfessionalId == leticia, Ct);
        await Assert.ThrowsAsync<BusinessException>(() => scoped.DeleteAsync(other.Id, Ct));
    }

    [Fact]
    public async Task Disponibilidade_gera_aviso_e_rejeita_sobreposicao()
    {
        using var db = _database.CreateContext();
        var (ana, _, leticia) = await SeedAsync(db);
        var schedule = new ScheduleService(db, new SystemActor { AccessLevel = AccessLevel.Administrador }, new AccessScope(db, new SystemActor()), _clock);

        await Assert.ThrowsAsync<BusinessException>(() => schedule.SaveAvailabilityAsync(leticia,
        [
            new(DayOfWeek.Tuesday, new(8, 0), new(12, 0)),
            new(DayOfWeek.Tuesday, new(11, 0), new(14, 0)),
        ], Ct));

        await schedule.SaveAvailabilityAsync(leticia, [new(DayOfWeek.Tuesday, new(8, 0), new(12, 0))], Ct);

        var inside = await Appointments(db).CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(9)), Ct);
        var outside = await Appointments(db).CreateAsync(Input(ana, leticia, Monday.AddDays(1).AddHours(19)), Ct);

        Assert.Empty(inside.Warnings);
        Assert.Single(outside.Warnings);
    }

    // ------------------------------------------------------------------

    private AppointmentService Appointments(MindCareDbContext db) => new(db, _clock);

    private static AppointmentInput Input(int clientId, int professionalId, DateTime start) =>
        new(clientId, AppointmentModality.Presencial, start, null, 150, 60) { ProfessionalId = professionalId };

    private static async Task<(int Ana, int Bia, int Leticia)> SeedAsync(MindCareDbContext db)
    {
        var ana = new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino" };
        var bia = new Client { Name = "Bia", Cpf = "11144477735", Gender = "Feminino" };
        db.Clients.AddRange(ana, bia);
        await db.SaveChangesAsync(Ct);
        return (ana.Id, bia.Id, await AddProfessionalAsync(db, "Letícia", "15350946056"));
    }

    private static async Task<int> AddProfessionalAsync(MindCareDbContext db, string name, string cpf)
    {
        var professional = new Professional
        {
            Name = name,
            Cpf = cpf,
            Gender = "Outro",
            Speciality = "Psicoterapia",
            User = new User { Username = cpf, PasswordHash = "x" },
        };
        db.Professionals.Add(professional);
        await db.SaveChangesAsync(Ct);
        return professional.Id;
    }

    public void Dispose() => _database.Dispose();
}
