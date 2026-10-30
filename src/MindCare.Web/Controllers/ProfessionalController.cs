using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

[Authorize(Policy = Policies.Admin)]
public sealed class ProfessionalController(IProfessionalService professionals) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await professionals.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProfessionalInput input, CancellationToken ct)
    {
        var created = await professionals.CreateAsync(input.ToDomain(), ct);
        return Success(
            $"Profissional cadastrado. Login: {created.Username} · Senha inicial: {created.InitialPassword}",
            created);
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] ProfessionalInput input, CancellationToken ct)
    {
        await professionals.UpdateAsync(input.ToDomain(id), ct);
        return Success("Profissional atualizado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await professionals.DeleteAsync(id, ct);
        return Success("Profissional e respectivo usuário excluídos.");
    }
}
