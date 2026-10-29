using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Notifications;

public enum PatientAction
{
    Confirm,
    Cancel,
}

/// <summary>Resumo da consulta exibido na página pública de confirmação.</summary>
public sealed record PublicAppointment(
    string PatientFirstName,
    string? ProfessionalName,
    DateTime ScheduledAt,
    int DurationMinutes,
    AppointmentModality Modality,
    AppointmentStatus Status,
    string? MeetingUrl,
    bool CanRespond);

public interface IPatientResponseService
{
    Task<PublicAppointment?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task<PublicAppointment> RespondAsync(string token, PatientAction action, string via, CancellationToken ct = default);
}

/// <summary>Confirmações e cancelamentos feitos pelo próprio paciente (link ou botão do WhatsApp).</summary>
internal sealed class PatientResponseService(
    MindCareDbContext db,
    IStaffNotificationService staff,
    TimeProvider clock) : IPatientResponseService
{
    public async Task<PublicAppointment?> FindByTokenAsync(string token, CancellationToken ct = default)
    {
        var appointment = await LoadAsync(token, ct);
        return appointment is null ? null : ToPublic(appointment);
    }

    public async Task<PublicAppointment> RespondAsync(string token, PatientAction action, string via, CancellationToken ct = default)
    {
        var appointment = await LoadAsync(token, ct) ?? throw new BusinessException("Link inválido ou expirado.");
        if (!CanRespond(appointment))
        {
            throw new BusinessException("Esta consulta não pode mais ser alterada por aqui. Entre em contato com a clínica.");
        }

        var now = clock.GetLocalNow().DateTime;
        appointment.PatientResponseVia = via;
        appointment.PatientRespondedAt = now;

        var who = appointment.Client?.Name ?? "Paciente";
        var when = appointment.ScheduledAt.ToString("dd/MM 'às' HH:mm", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        if (action == PatientAction.Confirm)
        {
            appointment.Status = AppointmentStatus.Confirmada;
            await staff.NotifyAppointmentAsync(appointment, "Consulta confirmada", $"{who} confirmou a consulta de {when} ({via}).", "bi-check2-circle", ct);
        }
        else
        {
            appointment.Status = AppointmentStatus.Cancelada;
            appointment.CancellationReason = $"Cancelada pelo paciente ({via})";
            appointment.ConfirmationTokenHash = null; // o link deixa de funcionar
            await staff.NotifyAppointmentAsync(appointment, "Consulta cancelada pelo paciente", $"{who} cancelou a consulta de {when} ({via}). O horário foi liberado.", "bi-x-circle", ct);
        }

        db.AuditLogs.Add(new AuditLog
        {
            Timestamp = now,
            Username = "paciente",
            Action = action == PatientAction.Confirm ? "Confirmação pelo paciente" : "Cancelamento pelo paciente",
            Entity = Audit.EntityName<Appointment>(),
            EntityId = appointment.Id.ToString(),
            Details = $"Via {via}",
        });

        await db.SaveChangesAsync(ct);
        return ToPublic(appointment);
    }

    private async Task<Appointment?> LoadAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64)
        {
            return null;
        }

        var hash = ConfirmationTokens.Hash(token);
        return await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Professional)
            .FirstOrDefaultAsync(a => a.ConfirmationTokenHash == hash, ct);
    }

    private bool CanRespond(Appointment a) =>
        a.ScheduledAt > clock.GetLocalNow().DateTime && a.Status is AppointmentStatus.Agendada or AppointmentStatus.Confirmada;

    private PublicAppointment ToPublic(Appointment a) => new(
        (a.Client?.Name ?? "paciente").Split(' ')[0],
        a.Professional?.Name,
        a.ScheduledAt,
        a.DurationMinutes,
        a.Modality,
        a.Status,
        a.Status == AppointmentStatus.Cancelada ? null : a.MeetingUrl,
        CanRespond(a));
}

/// <summary>Processa os eventos recebidos do webhook do WhatsApp Cloud API.</summary>
public interface IWhatsAppWebhookService
{
    bool IsValidSignature(string body, string? signatureHeader);
    bool VerifySubscription(string? mode, string? verifyToken);
    Task ProcessAsync(string body, CancellationToken ct = default);
}

internal sealed class WhatsAppWebhookService(
    MindCareDbContext db,
    IPatientResponseService responses,
    IStaffNotificationService staff,
    IOptions<WhatsAppOptions> options) : IWhatsAppWebhookService
{
    public bool VerifySubscription(string? mode, string? verifyToken) =>
        mode == "subscribe" && !string.IsNullOrEmpty(options.Value.VerifyToken) && verifyToken == options.Value.VerifyToken;

    /// <summary>Valida o cabeçalho X-Hub-Signature-256 (HMAC-SHA256 do corpo com o App Secret).</summary>
    public bool IsValidSignature(string body, string? signatureHeader)
    {
        var secret = options.Value.AppSecret;
        if (string.IsNullOrEmpty(secret))
        {
            // Sem App Secret só aceitamos no modo simulado (desenvolvimento).
            return options.Value.Mode == WhatsAppMode.Simulated;
        }

        if (signatureHeader is null || !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
        {
            return false;
        }

        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));
        byte[] received;
        try
        {
            received = Convert.FromHexString(signatureHeader["sha256=".Length..]);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, received);
    }

    public async Task ProcessAsync(string body, CancellationToken ct = default)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("entry", out var entries))
        {
            return;
        }

        foreach (var value in entries.EnumerateArray()
                     .Where(e => e.TryGetProperty("changes", out _))
                     .SelectMany(e => e.GetProperty("changes").EnumerateArray())
                     .Where(c => c.TryGetProperty("value", out _))
                     .Select(c => c.GetProperty("value")))
        {
            if (value.TryGetProperty("statuses", out var statuses))
            {
                await ApplyStatusesAsync(statuses, ct);
            }

            if (value.TryGetProperty("messages", out var messages))
            {
                foreach (var message in messages.EnumerateArray())
                {
                    await HandleIncomingAsync(message, ct);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyStatusesAsync(JsonElement statuses, CancellationToken ct)
    {
        foreach (var status in statuses.EnumerateArray())
        {
            var id = status.GetPropertyOrDefault("id");
            if (id is null)
            {
                continue;
            }

            var message = await db.OutboundMessages.FirstOrDefaultAsync(m => m.ExternalId == id, ct);
            if (message is null)
            {
                continue;
            }

            switch (status.GetPropertyOrDefault("status"))
            {
                case "delivered" when message.Status is MessageStatus.Enviada:
                    message.Status = MessageStatus.Entregue;
                    break;
                case "read":
                    message.Status = MessageStatus.Lida;
                    break;
                case "failed":
                    message.Status = MessageStatus.Falhou;
                    message.LastError = status.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0
                        ? errors[0].GetPropertyOrDefault("title") ?? "Falha na entrega"
                        : "Falha na entrega";
                    break;
            }
        }
    }

    private async Task HandleIncomingAsync(JsonElement message, CancellationToken ct)
    {
        var from = message.GetPropertyOrDefault("from");

        // Botões de resposta rápida dos templates: "CONFIRMAR:<token>" / "CANCELAR:<token>".
        var payload = message.TryGetProperty("button", out var button)
            ? button.GetPropertyOrDefault("payload")
            : message.TryGetProperty("interactive", out var interactive) && interactive.TryGetProperty("button_reply", out var reply)
                ? reply.GetPropertyOrDefault("id")
                : null;

        if (payload is not null && TryParsePayload(payload, out var action, out var token))
        {
            try
            {
                await responses.RespondAsync(token, action, "WhatsApp", ct);
            }
            catch (BusinessException)
            {
                // Link expirado ou consulta já alterada: avisa a equipe para responder manualmente.
                await NotifyTextAsync(from, $"Tentou {(action == PatientAction.Confirm ? "confirmar" : "cancelar")} uma consulta que não pode mais ser alterada.", ct);
            }

            return;
        }

        var text = message.TryGetProperty("text", out var t) ? t.GetPropertyOrDefault("body") : null;
        await NotifyTextAsync(from, string.IsNullOrWhiteSpace(text) ? "Enviou uma mensagem no WhatsApp." : $"“{Truncate(text, 180)}”", ct);
    }

    private async Task NotifyTextAsync(string? from, string message, CancellationToken ct)
    {
        var phone = Phone.Normalize(from);
        var client = phone is null ? null : await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Phone == phone, ct);
        var who = client?.Name ?? Phone.Format(phone);
        await staff.NotifyAdminsAsync($"Mensagem de {who}", message, client is null ? null : "/Client", "bi-whatsapp", ct);
    }

    private static bool TryParsePayload(string payload, out PatientAction action, out string token)
    {
        (action, token) = payload switch
        {
            _ when payload.StartsWith(MessageRenderer.ConfirmPrefix, StringComparison.Ordinal) => (PatientAction.Confirm, payload[MessageRenderer.ConfirmPrefix.Length..]),
            _ when payload.StartsWith(MessageRenderer.CancelPrefix, StringComparison.Ordinal) => (PatientAction.Cancel, payload[MessageRenderer.CancelPrefix.Length..]),
            _ => (PatientAction.Confirm, string.Empty),
        };
        return token.Length > 0;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "…";
}

internal static class JsonElementExtensions
{
    public static string? GetPropertyOrDefault(this JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
