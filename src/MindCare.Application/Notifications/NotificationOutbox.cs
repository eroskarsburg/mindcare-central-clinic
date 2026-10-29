using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Notifications;

/// <summary>Coloca mensagens para o paciente na fila de saída, na mesma transação da alteração da consulta.</summary>
public interface INotificationOutbox
{
    /// <summary>Adiciona as mensagens ao contexto; elas são gravadas no próximo SaveChanges.</summary>
    Task EnqueueAsync(Appointment appointment, MessageKind kind, string? note = null, CancellationToken ct = default);
}

internal sealed class NotificationOutbox(
    MindCareDbContext db,
    IOptions<NotificationOptions> options,
    IOptions<WhatsAppOptions> whatsApp,
    IOptions<EmailOptions> email,
    TimeProvider clock) : INotificationOutbox
{
    public async Task EnqueueAsync(Appointment appointment, MessageKind kind, string? note = null, CancellationToken ct = default)
    {
        if (kind != MessageKind.Lembrete && !options.Value.NotifyOnChanges)
        {
            return;
        }

        var client = appointment.Client ?? await db.Clients.FindAsync([appointment.ClientId], ct);
        if (client is null || client.IsAnonymized)
        {
            return;
        }

        var now = clock.GetLocalNow().DateTime;
        var channels = new List<(MessageChannel Channel, string Recipient)>();
        if (whatsApp.Value.IsEnabled && client.NotifyByWhatsApp && !string.IsNullOrWhiteSpace(client.Phone))
        {
            channels.Add((MessageChannel.WhatsApp, client.Phone));
        }

        if (email.Value.Enabled && client.NotifyByEmail && !string.IsNullOrWhiteSpace(client.Email))
        {
            channels.Add((MessageChannel.Email, client.Email));
        }

        if (channels.Count == 0)
        {
            return;
        }

        // Agendado/remarcado já dentro da janela do lembrete: esta mensagem faz as vezes do lembrete.
        if (kind is MessageKind.Agendamento or MessageKind.Remarcacao
            && appointment.ScheduledAt <= now.AddHours(options.Value.ReminderHoursBefore))
        {
            appointment.ReminderQueuedAt = now;
        }

        // Um novo token invalida links anteriores; cancelamentos não precisam de link.
        string? token = null;
        if (kind != MessageKind.Cancelamento)
        {
            token = ConfirmationTokens.New();
            appointment.ConfirmationTokenHash = ConfirmationTokens.Hash(token);
        }

        foreach (var (channel, recipient) in channels)
        {
            db.OutboundMessages.Add(new OutboundMessage
            {
                Client = client,
                Appointment = appointment,
                Channel = channel,
                Kind = kind,
                Recipient = recipient,
                ConfirmationToken = token,
                Note = note,
                CreatedAt = now,
                NextAttemptAt = now,
            });
        }
    }
}

/// <summary>Tokens dos links de confirmação: só o hash fica no banco.</summary>
public static class ConfirmationTokens
{
    public static string New() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
