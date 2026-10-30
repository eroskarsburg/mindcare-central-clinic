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

        var today = clock.GetLocalNow().Date;
        Client[] clients =
        [
            new() { Name = "Ana Beatriz Moura", Cpf = "52998224725", Gender = "Feminino", Age = 29 },
            new() { Name = "Carlos Eduardo Lima", Cpf = "11144477735", Gender = "Masculino", Age = 41 },
            new() { Name = "Helena Duarte", Cpf = "39053344705", Gender = "Feminino", Age = 34 },
            new() { Name = "Rafael Nogueira", Cpf = "86288366757", Gender = "Masculino", Age = 23 },
            new() { Name = "Sam Oliveira", Cpf = "71428793860", Gender = "Outro", Age = 31 },
        ];
        db.Clients.AddRange(clients);

        var professional = new Professional
        {
            Name = "Letícia Kasper",
            Cpf = "15350946056",
            Gender = "Feminino",
            Speciality = "Psicoterapia",
        };
        professional.User = new User
        {
            Username = professional.Cpf,
            PasswordHash = passwords.Hash("leticia" + professional.Cpf),
            AccessLevel = AccessLevel.Profissional,
        };
        db.Professionals.Add(professional);

        (int Client, int DayOffset, int Hour, AppointmentModality Modality, decimal Price, decimal Paid)[] plan =
        [
            (0, -6, 9, AppointmentModality.Presencial, 180, 180),
            (1, -3, 14, AppointmentModality.Teleconsulta, 150, 50),
            (2, -1, 16, AppointmentModality.Presencial, 180, 0),
            (3, 0, 10, AppointmentModality.Presencial, 160, 0),
            (4, 0, 15, AppointmentModality.Teleconsulta, 150, 150),
            (0, 1, 9, AppointmentModality.Presencial, 180, 0),
            (2, 3, 11, AppointmentModality.Teleconsulta, 180, 0),
            (1, 5, 17, AppointmentModality.Presencial, 150, 0),
        ];

        foreach (var (client, offset, hour, modality, price, paid) in plan)
        {
            var payment = new Payment { Price = price, PaidPrice = paid };
            payment.Reconcile(today.AddDays(Math.Min(offset, 0)));
            db.Appointments.Add(new Appointment
            {
                Client = clients[client],
                Modality = modality,
                ScheduledAt = today.AddDays(offset).AddHours(hour),
                Payment = payment,
            });
        }

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
