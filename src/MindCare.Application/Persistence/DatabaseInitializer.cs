using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MindCare.Application.Domain;
using MindCare.Application.Security;

namespace MindCare.Application.Persistence;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Aplica as migrations pendentes ao iniciar a aplicação.</summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;
}

public sealed class SeedOptions
{
    public const string Section = "Seed";

    /// <summary>Login do administrador criado quando não existe nenhum administrador.</summary>
    public string? AdminUsername { get; set; }

    /// <summary>Senha do administrador inicial. Sem ela, nenhum administrador é criado.</summary>
    public string? AdminPassword { get; set; }

    /// <summary>Popula a base vazia com pacientes e consultas fictícios.</summary>
    public bool DemoData { get; set; }
}

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}

internal sealed partial class DatabaseInitializer(
    MindCareDbContext db,
    IPasswordService passwords,
    IOptions<DatabaseOptions> database,
    IOptions<SeedOptions> seed,
    TimeProvider clock,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (database.Value.ApplyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync(ct);
        }

        await SeedAdminAsync(ct);

        if (seed.Value.DemoData)
        {
            await SeedDemoDataAsync(ct);
        }
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.AccessLevel == AccessLevel.Administrador, ct))
        {
            return;
        }

        var (username, password) = (seed.Value.AdminUsername, seed.Value.AdminPassword);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            LogNoAdmin(logger);
            return;
        }

        db.Users.Add(new User
        {
            Username = username.Trim(),
            PasswordHash = passwords.Hash(password),
            AccessLevel = AccessLevel.Administrador,
        });
        await db.SaveChangesAsync(ct);
        LogAdminCreated(logger, username);
    }

    private async Task SeedDemoDataAsync(CancellationToken ct)
    {
        if (await db.Clients.AnyAsync(ct))
        {
            return;
        }

        var now = clock.GetLocalNow().DateTime;
        var today = now.Date;
        Client[] clients =
        [
            new() { Name = "Ana Beatriz Moura", Phone = "5551999990001", Email = "ana.moura@exemplo.com", Cpf = "52998224725", Gender = "Feminino", BirthDate = today.AddYears(-29).AddDays(3), HealthInsurance = "Particular", EmergencyContactName = "Marta Moura (mãe)", EmergencyContactPhone = "5551988887777", ConsentAt = today.AddDays(-40) },
            new() { Name = "Carlos Eduardo Lima", Phone = "5551999990002", Email = "carlos.lima@exemplo.com", Cpf = "11144477735", Gender = "Masculino", BirthDate = new DateTime(1985, 3, 14), HealthInsurance = "Unimed", ConsentAt = today.AddDays(-20) },
            new() { Name = "Helena Duarte", Phone = "5551999990003", Email = "helena.duarte@exemplo.com", Cpf = "39053344705", Gender = "Feminino", BirthDate = new DateTime(1992, 7, 2), ConsentAt = today.AddDays(-15) },
            new() { Name = "Rafael Nogueira", Phone = "5551999990004", Cpf = "86288366757", Gender = "Masculino", Age = 23 },
            new() { Name = "Sam Oliveira", Phone = "5551999990005", Email = "sam.oliveira@exemplo.com", Cpf = "71428793860", Gender = "Outro", Age = 31, ConsentAt = today.AddDays(-5) },
        ];
        db.Clients.AddRange(clients);

        Professional NewProfessional(string name, string cpf, string gender, string speciality, string password) => new()
        {
            Name = name,
            Cpf = cpf,
            Gender = gender,
            Speciality = speciality,
            User = new User { Username = cpf, PasswordHash = passwords.Hash(password), AccessLevel = AccessLevel.Profissional },
        };

        var leticia = NewProfessional("Letícia Kasper", "15350946056", "Feminino", "Psicoterapia", "leticia" + "15350946056");
        var marcos = NewProfessional("Marcos Teixeira", "46873529010", "Masculino", "Psiquiatria", "marcos" + "46873529010");
        db.Professionals.AddRange(leticia, marcos);
        foreach (var (client, index) in clients.Select((c, i) => (c, i)))
        {
            client.ResponsibleProfessional = index == 3 ? marcos : leticia;
        }

        // Letícia: seg–sex 08–12 e 13–18 · Marcos: ter e qui 14–20
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            leticia.Availability.Add(new ProfessionalAvailability { DayOfWeek = day, StartTime = new(8, 0), EndTime = new(12, 0) });
            leticia.Availability.Add(new ProfessionalAvailability { DayOfWeek = day, StartTime = new(13, 0), EndTime = new(18, 0) });
        }

        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday })
        {
            marcos.Availability.Add(new ProfessionalAvailability { DayOfWeek = day, StartTime = new(14, 0), EndTime = new(20, 0) });
        }

        db.ScheduleBlocks.Add(new ScheduleBlock
        {
            StartsAt = today.AddDays(20),
            EndsAt = today.AddDays(21),
            Reason = "Feriado",
        });

        Appointment Session(Client client, Professional professional, DateTime start, AppointmentModality modality, decimal price, decimal paid, Guid? series = null)
        {
            var status = start < now
                ? AppointmentStatus.Realizada
                : start < today.AddDays(2) ? AppointmentStatus.Confirmada : AppointmentStatus.Agendada;
            var payment = new Payment { Price = price, PaidPrice = paid };
            payment.Reconcile(start < now ? start : today);
            return new Appointment
            {
                Client = client,
                Professional = professional,
                Modality = modality,
                ScheduledAt = start,
                Status = status,
                SeriesId = series,
                MeetingUrl = modality == AppointmentModality.Teleconsulta ? $"https://meet.jit.si/mindcare-demo-{Guid.NewGuid():N}" : null,
                Payment = payment,
            };
        }

        // Série semanal da Ana com a Letícia: 4 semanas para trás e 4 para frente, mesmo dia e horário.
        var series = Guid.NewGuid();
        var anchor = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Wednesday).AddHours(9);
        for (var week = -4; week <= 4; week++)
        {
            var start = anchor.AddDays(7 * week);
            db.Appointments.Add(Session(clients[0], leticia, start, AppointmentModality.Presencial, 180, start < now ? 180 : 0, series));
        }

        Appointment[] avulsas =
        [
            Session(clients[1], leticia, today.AddDays(-3).AddHours(14), AppointmentModality.Teleconsulta, 150, 50),
            Session(clients[2], leticia, today.AddDays(-1).AddHours(16), AppointmentModality.Presencial, 180, 0),
            Session(clients[3], marcos, today.AddDays(-2).AddHours(15), AppointmentModality.Presencial, 300, 300),
            Session(clients[3], leticia, today.AddHours(10), AppointmentModality.Presencial, 160, 0),
            Session(clients[4], leticia, today.AddHours(15), AppointmentModality.Teleconsulta, 150, 150),
            Session(clients[2], leticia, today.AddDays(3).AddHours(11), AppointmentModality.Teleconsulta, 180, 0),
            Session(clients[1], marcos, today.AddDays(5).AddHours(17), AppointmentModality.Presencial, 300, 0),
        ];
        db.Appointments.AddRange(avulsas);

        // Uma falta e um cancelamento para os indicadores.
        avulsas[1].Status = AppointmentStatus.Faltou;
        var cancelled = Session(clients[4], marcos, today.AddDays(2).AddHours(16), AppointmentModality.Presencial, 300, 0);
        cancelled.Status = AppointmentStatus.Cancelada;
        cancelled.CancellationReason = "Paciente viajando";
        db.Appointments.Add(cancelled);

        await db.SaveChangesAsync(ct);
        LogDemoSeeded(logger);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Nenhum administrador encontrado e Seed:AdminUsername/Seed:AdminPassword não configurados.")]
    private static partial void LogNoAdmin(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Administrador inicial '{Username}' criado.")]
    private static partial void LogAdminCreated(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dados de demonstração inseridos.")]
    private static partial void LogDemoSeeded(ILogger logger);
}
