using System.Globalization;
using System.Net;
using MindCare.Application.Domain;

namespace MindCare.Application.Notifications;

/// <summary>Conteúdo pronto para envio em qualquer canal.</summary>
public sealed record RenderedMessage(
    string Subject,
    string Text,
    string Html,
    IReadOnlyList<string> TemplateParameters,
    string? ConfirmUrl,
    string? ConfirmPayload,
    string? CancelPayload);

/// <summary>
/// Monta os textos das mensagens. Os parâmetros do WhatsApp seguem a ordem documentada em docs/whatsapp.md:
/// {{1}} nome · {{2}} profissional · {{3}} data · {{4}} horário · {{5}} modalidade/link · {{6}} complemento.
/// </summary>
public static class MessageRenderer
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public const string ConfirmPrefix = "CONFIRMAR:";
    public const string CancelPrefix = "CANCELAR:";

    public static RenderedMessage Render(OutboundMessage message, Appointment appointment, string clinicName, string publicBaseUrl)
    {
        var client = message.Client ?? appointment.Client;
        var firstName = (client?.Name ?? "paciente").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "paciente";
        var professional = appointment.Professional?.Name ?? clinicName;
        var date = appointment.ScheduledAt.ToString("dddd, dd/MM", PtBr);
        var time = appointment.ScheduledAt.ToString("HH:mm", PtBr);
        var where = appointment.Modality == AppointmentModality.Teleconsulta
            ? $"teleconsulta{(appointment.MeetingUrl is null ? string.Empty : $" ({appointment.MeetingUrl})")}"
            : "presencial";
        var note = string.IsNullOrWhiteSpace(message.Note) ? string.Empty : $" {message.Note}";

        var confirmUrl = message.ConfirmationToken is null
            ? null
            : $"{publicBaseUrl.TrimEnd('/')}/c/{message.ConfirmationToken}";

        var (subject, headline) = message.Kind switch
        {
            MessageKind.Agendamento => ($"Consulta agendada — {date} às {time}", "Sua consulta foi agendada"),
            MessageKind.Remarcacao => ($"Consulta remarcada — {date} às {time}", "Sua consulta foi remarcada"),
            MessageKind.Cancelamento => ($"Consulta cancelada — {date} às {time}", "Sua consulta foi cancelada"),
            _ => ($"Lembrete: consulta {date} às {time}", "Lembrete da sua consulta"),
        };

        var lines = new List<string>
        {
            $"Olá, {firstName}!",
            string.Empty,
            message.Kind == MessageKind.Cancelamento
                ? $"{headline}: {date} às {time} com {professional}.{note}"
                : $"{headline}: {date} às {time} com {professional} ({where}).{note}",
        };

        if (confirmUrl is not null)
        {
            lines.Add(string.Empty);
            lines.Add(message.Kind == MessageKind.Lembrete
                ? $"Confirme sua presença ou cancele com antecedência: {confirmUrl}"
                : $"Para confirmar ou cancelar: {confirmUrl}");
        }
        else if (message.Kind == MessageKind.Cancelamento)
        {
            lines.Add(string.Empty);
            lines.Add("Entre em contato com a clínica para remarcar.");
        }

        lines.Add(string.Empty);
        lines.Add($"— {clinicName}");
        var text = string.Join('\n', lines);

        return new RenderedMessage(
            subject,
            text,
            ToHtml(lines, confirmUrl, clinicName),
            [firstName, professional, date, time, where, note.Trim().Length == 0 ? "-" : note.Trim()],
            confirmUrl,
            message.ConfirmationToken is null ? null : ConfirmPrefix + message.ConfirmationToken,
            message.ConfirmationToken is null ? null : CancelPrefix + message.ConfirmationToken);
    }

    private static string ToHtml(IEnumerable<string> lines, string? confirmUrl, string clinicName)
    {
        var body = string.Join(string.Empty, lines
            .Where(l => confirmUrl is null || !l.Contains(confirmUrl, StringComparison.Ordinal))
            .Select(l => l.Length == 0 ? "<br>" : $"<p style=\"margin:0 0 8px\">{WebUtility.HtmlEncode(l)}</p>"));

        var button = confirmUrl is null
            ? string.Empty
            : $"<p style=\"margin:20px 0\"><a href=\"{WebUtility.HtmlEncode(confirmUrl)}\" style=\"background:#7d6049;color:#fff;padding:12px 22px;border-radius:12px;text-decoration:none;font-weight:600\">Confirmar ou cancelar</a></p>";

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;background:#f7f3ee;padding:24px">
              <div style="max-width:520px;margin:auto;background:#fff;border:1px solid #e8ddd0;border-radius:20px;padding:28px;color:#2e2723">
                <div style="font-family:Georgia,serif;font-size:22px;font-weight:600;color:#2f3e46;margin-bottom:16px">{WebUtility.HtmlEncode(clinicName)}</div>
                {body}
                {button}
              </div>
            </div>
            """;
    }
}
