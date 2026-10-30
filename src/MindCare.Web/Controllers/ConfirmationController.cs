using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MindCare.Application.Common;
using MindCare.Application.Notifications;

namespace MindCare.Web.Controllers;

/// <summary>Página pública em que o paciente confirma ou cancela a consulta pelo link recebido.</summary>
[AllowAnonymous]
[EnableRateLimiting(LoginController.RateLimitPolicy)]
[Route("c/{token}")]
public sealed class ConfirmationController(IPatientResponseService responses) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string token, CancellationToken ct)
    {
        var appointment = await responses.FindByTokenAsync(token, ct);
        return appointment is null ? View("Invalid") : View(appointment);
    }

    [HttpPost("confirmar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Confirm(string token, CancellationToken ct) => RespondAsync(token, PatientAction.Confirm, ct);

    [HttpPost("cancelar")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(string token, CancellationToken ct) => RespondAsync(token, PatientAction.Cancel, ct);

    private async Task<IActionResult> RespondAsync(string token, PatientAction action, CancellationToken ct)
    {
        try
        {
            var appointment = await responses.RespondAsync(token, action, "link", ct);
            ViewData["Done"] = action;
            return View("Index", appointment);
        }
        catch (BusinessException ex)
        {
            ViewData["Error"] = ex.Message;
            var appointment = await responses.FindByTokenAsync(token, ct);
            return appointment is null ? View("Invalid") : View("Index", appointment);
        }
    }
}
