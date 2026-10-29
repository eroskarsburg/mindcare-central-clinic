using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Domain;
using MindCare.Application.Notifications;
using MindCare.Web.Infrastructure;

namespace MindCare.Web.Controllers;

/// <summary>Avisos internos da equipe (sino) e histórico das mensagens enviadas aos pacientes.</summary>
public sealed class NotificationController(IStaffNotificationService staff, IMessageLogService messages) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await staff.ListAsync(CurrentUserId, 100, ct));

    [HttpPost]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await staff.MarkAllReadAsync(CurrentUserId, ct);
        return Success("Avisos marcados como lidos.");
    }

    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Messages(MessageStatus? status, MessageChannel? channel, CancellationToken ct)
    {
        ViewData["Status"] = status;
        ViewData["Channel"] = channel;
        return View(await messages.ListAsync(new MessageFilter(status, channel), ct));
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Retry(long id, CancellationToken ct)
    {
        await messages.RetryAsync(id, ct);
        return Success("Mensagem colocada novamente na fila de envio.");
    }
}

/// <summary>Sino de avisos no menu lateral.</summary>
public sealed class NotificationBellViewComponent(IStaffNotificationService staff) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.GetUserId();
        var latest = await staff.ListAsync(userId, 6);
        var unread = await staff.CountUnreadAsync(userId);
        return View((latest, unread));
    }
}
