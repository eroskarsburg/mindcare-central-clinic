using System.Security.Claims;
using MindCare.Application.Persistence;

namespace MindCare.Web.Infrastructure;

public static class Policies
{
    public const string Admin = "Admin";
}

/// <summary>Identidade da clínica exibida na interface (seção "Clinic").</summary>
public sealed class ClinicOptions
{
    public const string Section = "Clinic";

    public string Name { get; set; } = "MindCare";
    public string Tagline { get; set; } = "Cuidado que acolhe";
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.GivenName) ?? user.Identity?.Name ?? string.Empty;

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole(nameof(Application.Domain.AccessLevel.Administrador));

    public static string GetInitials(this ClaimsPrincipal user)
    {
        var parts = user.GetDisplayName().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant(),
        };
    }
}

internal static partial class DatabaseStartup
{
    /// <summary>
    /// Aplica migrations e seeds. Tenta algumas vezes, pois o MySQL pode demorar a aceitar conexões
    /// quando sobe junto com a aplicação (docker compose).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, int attempts = 10)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                LogRetry(logger, attempt, attempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Banco indisponível (tentativa {Attempt}/{Attempts}): {Reason}")]
    private static partial void LogRetry(ILogger logger, int attempt, int attempts, string reason);
}
