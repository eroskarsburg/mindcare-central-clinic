using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class ClientController(IClientService clients) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await clients.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientInput input, CancellationToken ct)
    {
        await clients.CreateAsync(input.ToDomain(), ct);
        return Success("Paciente cadastrado.");
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] ClientInput input, CancellationToken ct)
    {
        await clients.UpdateAsync(input.ToDomain(id), ct);
        return Success("Paciente atualizado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await clients.DeleteAsync(id, ct);
        return Success("Paciente excluído.");
    }
}
