using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Notifications;

public interface IStaffNotificationService
{
    /// <summary>Avisa os administradores e o profissional responsável pela consulta (gravado no próximo SaveChanges).</summary>
    Task NotifyAppointmentAsync(Appointment appointment, string title, string message, string icon, CancellationToken ct = default);

    Task NotifyAdminsAsync(string title, string message, string? link, string icon, CancellationToken ct = default);

    Task<IReadOnlyList<StaffNotification>> ListAsync(int userId, int limit = 50, CancellationToken ct = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
    Task MarkAllReadAsync(int userId, CancellationToken ct = default);
}

internal sealed class StaffNotificationService(MindCareDbContext db, TimeProvider clock) : IStaffNotificationService
{
    public async Task NotifyAppointmentAsync(Appointment appointment, string title, string message, string icon, CancellationToken ct = default)
    {
        var recipients = await AdminIdsAsync(ct);
        if (appointment.ProfessionalId is { } professionalId)
        {
            var userId = await db.Professionals.Where(p => p.Id == professionalId).Select(p => p.UserId).FirstOrDefaultAsync(ct);
            if (userId != 0)
            {
                recipients.Add(userId);
            }
        }

        Add(recipients, title, message, "/Appointment", icon);
    }

    public async Task NotifyAdminsAsync(string title, string message, string? link, string icon, CancellationToken ct = default) =>
        Add(await AdminIdsAsync(ct), title, message, link, icon);

    public async Task<IReadOnlyList<StaffNotification>> ListAsync(int userId, int limit = 50, CancellationToken ct = default) =>
        await db.StaffNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Id)
            .Take(limit)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
        db.StaffNotifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        await db.StaffNotifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
    }

    private async Task<HashSet<int>> AdminIdsAsync(CancellationToken ct) =>
        [.. await db.Users.Where(u => u.AccessLevel == AccessLevel.Administrador).Select(u => u.Id).ToListAsync(ct)];

    private void Add(IEnumerable<int> userIds, string title, string message, string? link, string icon)
    {
        var now = clock.GetLocalNow().DateTime;
        foreach (var userId in userIds.Distinct())
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = userId,
                Title = Trim(title, 120),
                Message = Trim(message, 500),
                Link = link,
                Icon = icon,
                CreatedAt = now,
            });
        }
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max];
}

public sealed record MessageFilter(MessageStatus? Status = null, MessageChannel? Channel = null, int Limit = 300);

/// <summary>Histórico da fila de mensagens para acompanhamento e reenvio.</summary>
public interface IMessageLogService
{
    Task<IReadOnlyList<OutboundMessage>> ListAsync(MessageFilter filter, CancellationToken ct = default);
    Task RetryAsync(long id, CancellationToken ct = default);
}

internal sealed class MessageLogService(MindCareDbContext db, TimeProvider clock) : IMessageLogService
{
    public async Task<IReadOnlyList<OutboundMessage>> ListAsync(MessageFilter filter, CancellationToken ct = default)
    {
        var query = db.OutboundMessages.AsNoTracking()
            .Include(m => m.Client)
            .Include(m => m.Appointment)
            .AsQueryable();

        if (filter.Status is { } status)
        {
            query = query.Where(m => m.Status == status);
        }

        if (filter.Channel is { } channel)
        {
            query = query.Where(m => m.Channel == channel);
        }

        return await query.OrderByDescending(m => m.Id).Take(filter.Limit).ToListAsync(ct);
    }

    /// <summary>Reenvia uma mensagem que falhou. Um novo link de confirmação é gerado quando necessário.</summary>
    public async Task RetryAsync(long id, CancellationToken ct = default)
    {
        var message = await db.OutboundMessages.Include(m => m.Appointment).FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new BusinessException("Mensagem não encontrada.");

        if (message.Status is not (MessageStatus.Falhou or MessageStatus.Ignorada))
        {
            throw new BusinessException("Só é possível reenviar mensagens com falha ou ignoradas.");
        }

        if (message.Kind != MessageKind.Cancelamento && message.Appointment is { } appointment)
        {
            var token = ConfirmationTokens.New();
            appointment.ConfirmationTokenHash = ConfirmationTokens.Hash(token);
            message.ConfirmationToken = token;
        }

        message.Status = MessageStatus.Pendente;
        message.Attempts = 0;
        message.LastError = null;
        message.NextAttemptAt = clock.GetLocalNow().DateTime;
        await db.SaveChangesAsync(ct);
    }
}
