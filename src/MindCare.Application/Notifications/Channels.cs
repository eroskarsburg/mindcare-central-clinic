using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MindCare.Application.Domain;

namespace MindCare.Application.Notifications;

/// <summary>Resultado do envio: id no provedor (quando houver).</summary>
public sealed record SendResult(string? ExternalId);

public interface IMessageChannel
{
    MessageChannel Channel { get; }
    Task<SendResult> SendAsync(OutboundMessage message, RenderedMessage content, CancellationToken ct);
}

/// <summary>Falha permanente (ex.: número inválido): não adianta tentar de novo.</summary>
public sealed class PermanentDeliveryException : Exception
{
    public PermanentDeliveryException() : base("Falha permanente no envio.") { }

    public PermanentDeliveryException(string message) : base(message) { }

    public PermanentDeliveryException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// WhatsApp Cloud API (Meta). Envia mensagens por template aprovado, com botões de resposta rápida
/// "Confirmar"/"Cancelar" cujo payload volta pelo webhook.
/// </summary>
internal sealed partial class WhatsAppChannel(
    HttpClient http,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppChannel> logger) : IMessageChannel
{
    public MessageChannel Channel => MessageChannel.WhatsApp;

    public async Task<SendResult> SendAsync(OutboundMessage message, RenderedMessage content, CancellationToken ct)
    {
        var o = options.Value;
        var payload = BuildPayload(message, content, o);

        if (o.Mode == WhatsAppMode.Simulated)
        {
            LogSimulated(logger, message.Recipient, new JsonText(payload));
            return new SendResult($"simulado-{Guid.NewGuid():N}");
        }

        if (string.IsNullOrWhiteSpace(o.PhoneNumberId) || string.IsNullOrWhiteSpace(o.AccessToken))
        {
            throw new PermanentDeliveryException("WhatsApp não configurado: defina WhatsApp__PhoneNumberId e WhatsApp__AccessToken.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{o.ApiBaseUrl.TrimEnd('/')}/{o.ApiVersion}/{o.PhoneNumberId}/messages")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.AccessToken);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = ExtractError(body) ?? $"HTTP {(int)response.StatusCode}";
            // 4xx (exceto limite de taxa) indicam problema no pedido: número, template ou permissão.
            if ((int)response.StatusCode is >= 400 and < 500 and not 429)
            {
                throw new PermanentDeliveryException($"WhatsApp recusou a mensagem: {error}");
            }

            throw new HttpRequestException($"WhatsApp indisponível: {error}");
        }

        var id = JsonNode.Parse(body)?["messages"]?[0]?["id"]?.GetValue<string>();
        return new SendResult(id);
    }

    internal static JsonObject BuildPayload(OutboundMessage message, RenderedMessage content, WhatsAppOptions o)
    {
        var template = message.Kind switch
        {
            MessageKind.Agendamento => o.Templates.Agendamento,
            MessageKind.Remarcacao => o.Templates.Remarcacao,
            MessageKind.Cancelamento => o.Templates.Cancelamento,
            _ => o.Templates.Lembrete,
        };

        var components = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "body",
                ["parameters"] = new JsonArray([.. content.TemplateParameters.Select(p => (JsonNode)new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = p,
                })]),
            },
        };

        if (content.ConfirmPayload is not null && content.CancelPayload is not null)
        {
            components.Add(QuickReply(0, content.ConfirmPayload));
            components.Add(QuickReply(1, content.CancelPayload));
        }

        return new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = message.Recipient,
            ["type"] = "template",
            ["template"] = new JsonObject
            {
                ["name"] = template,
                ["language"] = new JsonObject { ["code"] = o.LanguageCode },
                ["components"] = components,
            },
        };
    }

    private static JsonObject QuickReply(int index, string payload) => new()
    {
        ["type"] = "button",
        ["sub_type"] = "quick_reply",
        ["index"] = index.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["parameters"] = new JsonArray(new JsonObject { ["type"] = "payload", ["payload"] = payload }),
    };

    private static string? ExtractError(string body)
    {
        try
        {
            var error = JsonNode.Parse(body)?["error"];
            return error is null ? null : $"{error["message"]?.GetValue<string>()} (código {error["code"]})";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[WhatsApp simulado] Para {Recipient}: {Payload}")]
    private static partial void LogSimulated(ILogger logger, string recipient, JsonText payload);

    /// <summary>Adia a serialização para quando o log realmente for escrito.</summary>
    internal readonly record struct JsonText(JsonNode Node)
    {
        public override string ToString() => Node.ToJsonString();
    }
}

/// <summary>E-mail via SMTP (MailKit) ou gravação em pasta (.eml) em desenvolvimento.</summary>
internal sealed class EmailChannel(IOptions<EmailOptions> options) : IMessageChannel
{
    public MessageChannel Channel => MessageChannel.Email;

    public async Task<SendResult> SendAsync(OutboundMessage message, RenderedMessage content, CancellationToken ct)
    {
        var o = options.Value;
        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(o.FromName, o.FromAddress));
        if (!MailboxAddress.TryParse(message.Recipient, out var to))
        {
            throw new PermanentDeliveryException($"E-mail inválido: {message.Recipient}");
        }

        mime.To.Add(to);
        mime.Subject = content.Subject;
        mime.Body = new BodyBuilder { TextBody = content.Text, HtmlBody = content.Html }.ToMessageBody();

        if (!string.IsNullOrWhiteSpace(o.PickupDirectory))
        {
            Directory.CreateDirectory(o.PickupDirectory);
            var path = Path.Combine(o.PickupDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}-{message.Id}-{message.Kind}.eml");
            await mime.WriteToAsync(path, ct);
            return new SendResult(mime.MessageId);
        }

        if (string.IsNullOrWhiteSpace(o.Host))
        {
            throw new PermanentDeliveryException("E-mail não configurado: defina Email__Host.");
        }

        using var smtp = new SmtpClient();
        var security = !o.UseTls ? SecureSocketOptions.None : o.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await smtp.ConnectAsync(o.Host, o.Port, security, ct);
        if (!string.IsNullOrWhiteSpace(o.Username))
        {
            await smtp.AuthenticateAsync(o.Username, o.Password ?? string.Empty, ct);
        }

        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(true, ct);
        return new SendResult(mime.MessageId);
    }
}
