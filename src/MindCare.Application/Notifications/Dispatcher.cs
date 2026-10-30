using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Notifications;

public sealed class ClinicIdentity
{
    public const string Section = "Clinic";

    public string Name { get; set; } = "MindCare";
}

/// <summary>Agenda lembretes das próximas consultas e envia as mensagens pendentes da fila.</summary>
public interface INotificationDispatcher
{
    Task<int> QueueDueRemindersAsync(CancellationToken ct = default);
    Task<int> DispatchPendingAsync(int batchSize = 50, CancellationToken ct = default);
}

internal sealed partial class NotificationDispatcher(
    MindCareDbContext db,
    INotificationOutbox outbox,
    IEnumerable<IMessageChannel> channels,
    IOptions<NotificationOptions> options,
    IOptions<ClinicIdentity> clinic,
    TimeProvider clock,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task<int> QueueDueRemindersAsync(CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        var limit = now.AddHours(options.Value.ReminderHoursBefore);

        var due = await db.Appointments
            .Include(a => a.Client)
            .Where(a => a.ScheduledAt > now && a.ScheduledAt <= limit && a.ReminderQueuedAt == null
                        && (a.Status == AppointmentStatus.Agendada || a.Status == AppointmentStatus.Confirmada))
            .ToListAsync(ct);

        foreach (var appointment in due)
        {
            appointment.ReminderQueuedAt = now;
            await outbox.EnqueueAsync(appointment, MessageKind.Lembrete, ct: ct);
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return due.Count;
    }

    public async Task<int> DispatchPendingAsync(int batchSize = 50, CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        var pending = await db.OutboundMessages
            .Include(m => m.Client)
            .Include(m => m.Appointment!).ThenInclude(a => a.Professional)
            .Where(m => m.Status == MessageStatus.Pendente && m.NextAttemptAt <= now)
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        var byChannel = channels.ToDictionary(c => c.Channel);
        foreach (var message in pending)
        {
            await SendOneAsync(message, byChannel, ct);
            await db.SaveChangesAsync(ct); // grava cada envio: evita reenvio se o processo cair no meio do lote
        }

        return pending.Count;
    }

    private async Task SendOneAsync(OutboundMessage message, Dictionary<MessageChannel, IMessageChannel> byChannel, CancellationToken ct)
    {
        var now = clock.GetLocalNow().DateTime;
        var appointment = message.Appointment;

        // Não faz sentido lembrar ou confirmar consultas que já passaram ou foram canceladas depois de entrar na fila.
        if (appointment is null
            || (message.Kind != MessageKind.Cancelamento && appointment.Status == AppointmentStatus.Cancelada)
            || appointment.ScheduledAt < now)
        {
            message.Status = MessageStatus.Ignorada;
            message.LastError = "Consulta cancelada, removida ou já realizada antes do envio.";
            message.ConfirmationToken = null;
            return;
        }

        if (!byChannel.TryGetValue(message.Channel, out var channel))
        {
            message.Status = MessageStatus.Ignorada;
            message.LastError = "Canal desativado.";
            message.ConfirmationToken = null;
            return;
        }

        try
        {
            var content = MessageRenderer.Render(message, appointment, clinic.Value.Name, options.Value.PublicBaseUrl);
            var result = await channel.SendAsync(message, content, ct);
            message.MarkSent(result.ExternalId, now);
        }
        catch (PermanentDeliveryException ex)
        {
            message.Attempts = OutboundMessage.MaxAttempts - 1;
            message.MarkFailed(ex.Message, now);
            LogFailure(logger, message.Id, message.Channel, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.MarkFailed(ex.Message, now);
            LogFailure(logger, message.Id, message.Channel, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao enviar mensagem {Id} por {Channel}: {Error}")]
    private static partial void LogFailure(ILogger logger, long id, MessageChannel channel, string error);
}

/// <summary>Executa periodicamente o agendamento de lembretes e o envio da fila.</summary>
internal sealed partial class NotificationWorker(
    IServiceScopeFactory scopes,
    IOptions<NotificationOptions> options,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.WorkerIntervalSeconds)));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                var reminders = await dispatcher.QueueDueRemindersAsync(stoppingToken);
                var sent = await dispatcher.DispatchPendingAsync(ct: stoppingToken);
                if (reminders + sent > 0)
                {
                    LogCycle(logger, reminders, sent);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogError(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notificações: {Reminders} lembrete(s) agendado(s), {Sent} mensagem(ns) processada(s).")]
    private static partial void LogCycle(ILogger logger, int reminders, int sent);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro no processamento de notificações.")]
    private static partial void LogError(ILogger logger, Exception exception);
}
