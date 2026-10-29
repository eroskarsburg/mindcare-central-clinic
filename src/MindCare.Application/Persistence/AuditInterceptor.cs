using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MindCare.Application.Domain;
using MindCare.Application.Security;

namespace MindCare.Application.Persistence;

/// <summary>
/// Registra automaticamente criações, alterações e exclusões de entidades com dados pessoais ou de saúde.
/// Guarda apenas os nomes dos campos alterados, nunca os valores.
/// </summary>
internal sealed class AuditInterceptor(ICurrentActor actor, TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties =
        [nameof(User.LastActivity), nameof(User.FailedLoginCount), nameof(User.LockoutUntil)];

    private List<PendingEntry>? _pending;
    private bool _writingAudit;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is MindCareDbContext db && Flush(db))
        {
            await db.SaveChangesAsync(cancellationToken);
            _writingAudit = false;
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is MindCareDbContext db && Flush(db))
        {
            db.SaveChanges();
            _writingAudit = false;
        }

        return base.SavedChanges(eventData, result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending = null;

    private void Capture(DbContext? context)
    {
        if (context is null || _writingAudit)
        {
            return;
        }

        _pending = [];
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!Audit.IsAudited(entry.Entity) || entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var (action, details) = entry.State switch
            {
                EntityState.Added => (AuditActions.Created, null),
                EntityState.Deleted => (AuditActions.Deleted, null),
                _ => (AuditActions.Updated, ChangedProperties(entry)),
            };

            // Alterações apenas em campos técnicos (ex.: último acesso) não geram registro.
            if (action == AuditActions.Updated && string.IsNullOrEmpty(details))
            {
                continue;
            }

            _pending.Add(new PendingEntry(entry, action, details,
                entry.State == EntityState.Added ? null : KeyOf(entry)));
        }
    }

    private bool Flush(MindCareDbContext db)
    {
        if (_pending is not { Count: > 0 } pending)
        {
            return false;
        }

        _pending = null;
        _writingAudit = true;
        var now = clock.GetLocalNow().DateTime;
        foreach (var p in pending)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Timestamp = now,
                UserId = actor.UserId,
                Username = actor.Username,
                IpAddress = actor.IpAddress,
                Action = p.Action,
                Entity = Audit.EntityName(p.Entry.Entity),
                EntityId = p.Key ?? KeyOf(p.Entry), // chaves geradas só existem após salvar
                Details = p.Details,
            });
        }

        return true;
    }

    private static string? ChangedProperties(EntityEntry entry)
    {
        var names = entry.Properties
            .Where(p => p.IsModified && !IgnoredProperties.Contains(p.Metadata.Name))
            .Select(p => p.Metadata.Name == nameof(User.PasswordHash) ? "Senha" : p.Metadata.Name)
            .ToList();
        return names.Count == 0 ? null : "Campos: " + string.Join(", ", names);
    }

    private static string? KeyOf(EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .FirstOrDefault();

    private sealed record PendingEntry(EntityEntry Entry, string Action, string? Details, string? Key);
}

/// <summary>Quais entidades são auditadas e como aparecem no log.</summary>
public static class Audit
{
    private static readonly Dictionary<Type, string> Names = new()
    {
        [typeof(Client)] = "Paciente",
        [typeof(Appointment)] = "Consulta",
        [typeof(Payment)] = "Pagamento",
        [typeof(Professional)] = "Profissional",
        [typeof(User)] = "Usuário",
    };

    public static void Register<T>(string displayName) => Names[typeof(T)] = displayName;

    public static bool IsAudited(object entity) => Names.ContainsKey(entity.GetType());

    public static string EntityName(object entity) =>
        Names.TryGetValue(entity.GetType(), out var name) ? name : entity.GetType().Name;

    public static string EntityName<T>() => Names.TryGetValue(typeof(T), out var name) ? name : typeof(T).Name;
}
