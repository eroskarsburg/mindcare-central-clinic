using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application;

public static class DependencyInjection
{
    public const string ConnectionStringName = "MindCare";

    public static IServiceCollection AddMindCareApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' não configurada. " +
                "Defina a variável de ambiente ConnectionStrings__MindCare (veja .env.example).");
        }

        // O host pode registrar o próprio ICurrentActor (ex.: usuário da requisição HTTP) antes desta chamada.
        services.TryAddScoped<ICurrentActor, SystemActor>();
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<MindCareDbContext>((sp, options) => options
            .UseMySQL(connectionString)
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.Section));
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.Section));
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection(SecurityOptions.Section));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProfessionalService, ProfessionalService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddHealthChecks().AddDbContextCheck<MindCareDbContext>("database");

        return services;
    }
}
