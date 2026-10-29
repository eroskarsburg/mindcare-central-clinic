using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MindCare.Application.Records;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class ClientController(
    IClientService clients,
    IProfessionalService professionals,
    IClinicalNoteService notes,
    IClientDocumentService documents,
    IOptions<StorageOptions> storage) : AppController
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new ClientsPageViewModel(await clients.ListAsync(ct), await professionals.ListAsync(ct)));

    /// <summary>Ficha do paciente: dados, histórico, financeiro, prontuário e documentos.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var details = await clients.GetDetailsAsync(id, ct);
        return View(new ClientDetailsViewModel(
            details,
            await notes.ListAsync(id, ct),
            await documents.ListAsync(id, ct),
            await professionals.ListAsync(ct),
            storage.Value.MaxUploadMegabytes));
    }

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
