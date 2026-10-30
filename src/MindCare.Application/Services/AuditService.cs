using Microsoft.EntityFrameworkCore;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed record AuditFilter(DateTime? From, DateTime? To, string? Entity, string? Action, int Limit = 500);

public interface IAuditService
{
    /// <summary>Registra eventos que não passam pelo SaveChanges (leituras, logins, exportações).</summary>
    Task LogAsync(string action, string entity, string? entityId = null, string? details = null, CancellationToken ct = default);

    Task<IReadOnlyList<AuditLog>> ListAsync(AuditFilter filter, CancellationToken ct = default);
}

internal sealed class AuditService(MindCareDbContext db, ICurrentActor actor, TimeProvider clock) : IAuditService
{
    public async Task LogAsync(string action, string entity, string? entityId = null, string? details = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Timestamp = clock.GetLocalNow().DateTime,
            UserId = actor.UserId,
            Username = actor.Username,
            IpAddress = actor.IpAddress,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLog>> ListAsync(AuditFilter filter, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (filter.From is { } from)
        {
            query = query.Where(a => a.Timestamp >= from);
        }

        if (filter.To is { } to)
        {
            var end = to.Date.AddDays(1);
            query = query.Where(a => a.Timestamp < end);
        }

        if (!string.IsNullOrWhiteSpace(filter.Entity))
        {
            query = query.Where(a => a.Entity == filter.Entity);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        return await query.OrderByDescending(a => a.Id).Take(filter.Limit).ToListAsync(ct);
    }
}
