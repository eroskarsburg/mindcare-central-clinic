using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;

namespace MindCare.Web.Controllers;

public sealed class HomeController(IDashboardService dashboard) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dashboard.GetAsync(ct));
}
