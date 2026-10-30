using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<MindCareDbContext>(options => options.UseMySQL(connectionString));

        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.Section));
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.Section));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
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
