using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class PaymentController(IPaymentService payments) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await payments.ListAsync(ct));

    [HttpPut]
    public async Task<IActionResult> Register(int id, [FromBody] PaymentInput input, CancellationToken ct)
    {
        await payments.RegisterAsync(id, input.PaidPrice, input.PaidDate, ct);
        return Success("Pagamento registrado.");
    }
}
