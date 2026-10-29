using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class ClientController(IClientService clients) : AppController
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await clients.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientInput input, CancellationToken ct)
    {
        await clients.CreateAsync(input.ToDomain(), input.ConsentGiven, ct);
        return Success("Paciente cadastrado.");
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] ClientInput input, CancellationToken ct)
    {
        await clients.UpdateAsync(input.ToDomain(id), input.ConsentGiven, ct);
        return Success("Paciente atualizado.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await clients.DeleteAsync(id, ct);
        return Success("Paciente excluído.");
    }

    /// <summary>Exporta os dados do titular em JSON (LGPD, art. 18).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        var export = await clients.ExportAsync(id, ct);
        return File(JsonSerializer.SerializeToUtf8Bytes(export, ExportJson), "application/json", $"paciente-{id}-dados.json");
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> Anonymize(int id, CancellationToken ct)
    {
        await clients.AnonymizeAsync(id, ct);
        return Success("Dados pessoais anonimizados. O histórico foi preservado sem identificação.");
    }
}
