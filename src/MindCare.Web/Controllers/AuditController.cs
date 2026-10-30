using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;

namespace MindCare.Web.Controllers;

[Authorize(Policy = Policies.Admin)]
public sealed class AuditController(IAuditService audit) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? entity, string? action, CancellationToken ct)
    {
        var filter = new AuditFilter(from ?? DateTime.Today.AddDays(-30), to, entity, action);
        ViewData["Filter"] = filter;
        return View(await audit.ListAsync(filter, ct));
    }
}
