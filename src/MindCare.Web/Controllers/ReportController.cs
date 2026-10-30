using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Common;
using MindCare.Application.Finance;
using MindCare.Application.Security;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

/// <summary>Relatórios financeiros e de agenda, exportação para Excel e recibos mensais.</summary>
public sealed class ReportController(
    IReportService reports,
    IReceiptService receipts,
    IProfessionalService professionals,
    IClientService clients,
    IAccessScope scope) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? professionalId, CancellationToken ct)
    {
        var (start, end) = Period(from, to);
        var report = await reports.GetAsync(start, end, professionalId, ct);
        var restricted = await scope.RestrictedProfessionalIdAsync(ct);
        var allClients = await clients.ListAsync(ct);

        return View(new ReportPageViewModel(
            report,
            await professionals.ListAsync(ct),
            [.. allClients.Where(c => !c.IsAnonymized)],
            restricted ?? professionalId,
            restricted is null));
    }

    [HttpGet]
    public async Task<IActionResult> Export(DateTime? from, DateTime? to, int? professionalId, CancellationToken ct)
    {
        var (start, end) = Period(from, to);
        var report = await reports.GetAsync(start, end, professionalId, ct);
        return File(reports.ToExcel(report),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"mindcare-relatorio-{start:yyyy-MM-dd}-a-{end.AddDays(-1):yyyy-MM-dd}.xlsx");
    }

    /// <summary>Recibo mensal consolidado do paciente (IR). Mês no formato yyyy-MM.</summary>
    [HttpGet]
    public async Task<IActionResult> MonthlyReceipt(int clientId, string month, CancellationToken ct)
    {
        if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return BadRequest();
        }

        try
        {
            var receipt = await receipts.MonthlyAsync(clientId, date.Year, date.Month, ct);
            return File(receipt.Content, "application/pdf", receipt.FileName);
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Client", new { id = clientId });
        }
    }

    /// <summary>Período padrão: mês atual. "to" é inclusivo na interface e exclusivo no serviço.</summary>
    private static (DateTime From, DateTime To) Period(DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? start.AddMonths(1).AddDays(-1)).Date.AddDays(1);
        return end <= start ? (start, start.AddDays(1)) : (start, end);
    }
}

[Authorize(Policy = Policies.Admin)]
public sealed class ServiceController(IPaymentService payments) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await payments.ListServiceTypesAsync(ct: ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceTypeInput input, CancellationToken ct)
    {
        await payments.SaveServiceTypeAsync(input.ToDomain(), ct);
        return Success("Tipo de serviço criado.");
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceTypeInput input, CancellationToken ct)
    {
        await payments.SaveServiceTypeAsync(input.ToDomain(id), ct);
        return Success("Tipo de serviço atualizado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await payments.DeleteServiceTypeAsync(id, ct);
        return Success("Tipo de serviço removido (ou desativado, se já usado em consultas).");
    }
}
