using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Notifications;

namespace MindCare.Web.Controllers;

/// <summary>
/// Webhook da WhatsApp Cloud API: verificação inicial (GET) e eventos (POST) — respostas dos botões,
/// mensagens recebidas e status de entrega. Configure a URL /webhooks/whatsapp no painel da Meta.
/// </summary>
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ApiController]
[Route("webhooks/whatsapp")]
public sealed partial class WhatsAppWebhookController(IWhatsAppWebhookService webhook, ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge) =>
        webhook.VerifySubscription(mode, verifyToken) ? Content(challenge ?? string.Empty, "text/plain") : Forbid();

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (!webhook.IsValidSignature(body, Request.Headers["X-Hub-Signature-256"]))
        {
            LogInvalidSignature(logger);
            return Unauthorized();
        }

        try
        {
            await webhook.ProcessAsync(body, ct);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest();
        }

        // A Meta reenvia eventos que não recebem 200: respondemos rápido mesmo sem nada a fazer.
        return Ok();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook do WhatsApp recusado: assinatura inválida.")]
    private static partial void LogInvalidSignature(ILogger logger);
}
