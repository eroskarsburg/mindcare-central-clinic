using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MindCare.Application.Finance;
using MindCare.Application.Notifications;
using MindCare.Application.Persistence;
using MindCare.Application.Records;
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
        services.AddOptions<TeleconsultOptions>().Bind(configuration.GetSection(TeleconsultOptions.Section));
        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection(NotificationOptions.Section));
        services.AddOptions<WhatsAppOptions>().Bind(configuration.GetSection(WhatsAppOptions.Section));
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.Section));
        services.AddOptions<ClinicIdentity>().Bind(configuration.GetSection(ClinicIdentity.Section));
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.Section));
        services.AddOptions<ClinicLegalOptions>().Bind(configuration.GetSection(ClinicLegalOptions.Section));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IAccessScope, AccessScope>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IProfessionalService, ProfessionalService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Financeiro: recibos (PDF) e relatórios (Excel).
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IReportService, ReportService>();

        // Prontuário (criptografado com Data Protection, registrado pelo host) e documentos.
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IClinicalNoteService, ClinicalNoteService>();
        services.AddScoped<IClientDocumentService, ClientDocumentService>();

        // Notificações: fila (outbox), canais de envio, respostas do paciente e processamento em segundo plano.
        services.AddScoped<INotificationOutbox, NotificationOutbox>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IStaffNotificationService, StaffNotificationService>();
        services.AddScoped<IMessageLogService, MessageLogService>();
        services.AddScoped<IPatientResponseService, PatientResponseService>();
        services.AddScoped<IWhatsAppWebhookService, WhatsAppWebhookService>();
        services.AddHttpClient<WhatsAppChannel>(http => http.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<IMessageChannel>(sp => sp.GetRequiredService<WhatsAppChannel>());
        services.AddScoped<IMessageChannel, EmailChannel>();
        services.AddHostedService<NotificationWorker>();

        services.AddHealthChecks().AddDbContextCheck<MindCareDbContext>("database");

        return services;
    }
}
