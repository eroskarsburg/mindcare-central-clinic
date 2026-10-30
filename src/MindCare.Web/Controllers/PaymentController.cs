using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Common;
using MindCare.Application.Finance;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class PaymentController(IPaymentService payments, IReceiptService receipts) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await payments.ListAsync(ct));

    /// <summary>Registra um recebimento (parcela) da consulta.</summary>
    [HttpPost]
    public async Task<IActionResult> Receive(int id, [FromBody] EntryFormInput input, CancellationToken ct)
    {
        await payments.AddEntryAsync(id, input.ToInput(), ct);
        return Success("Recebimento registrado.");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEntry(long id, CancellationToken ct)
    {
        await payments.DeleteEntryAsync(id, ct);
        return Success("Recebimento removido.");
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        try
        {
            var receipt = await receipts.ForPaymentAsync(id, ct);
            return File(receipt.Content, "application/pdf", receipt.FileName);
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

public sealed class PackageController(IPaymentService payments) : AppController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PackageFormInput input, CancellationToken ct)
    {
        await payments.CreatePackageAsync(input.ToInput(), ct);
        return Success("Pacote criado.");
    }

    [HttpPost]
    public async Task<IActionResult> Receive(int id, [FromBody] EntryFormInput input, CancellationToken ct)
    {
        await payments.AddPackageEntryAsync(id, input.ToInput(), ct);
        return Success("Pagamento do pacote registrado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await payments.DeletePackageAsync(id, ct);
        return Success("Pacote excluído.");
    }
}
