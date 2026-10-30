using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Common;
using MindCare.Application.Records;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

/// <summary>Prontuário (evoluções) e documentos do paciente.</summary>
public sealed class RecordController(IClinicalNoteService notes, IClientDocumentService documents) : AppController
{
    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] NoteInput input, CancellationToken ct)
    {
        await notes.CreateAsync(input.ClientId, input.AppointmentId, input.Content, ct);
        return Success("Evolução registrada no prontuário.");
    }

    [HttpPut]
    public async Task<IActionResult> UpdateNote(long id, [FromBody] NoteInput input, CancellationToken ct)
    {
        await notes.UpdateAsync(id, input.Content, ct);
        return Success("Evolução atualizada.");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteNote(long id, CancellationToken ct)
    {
        await notes.DeleteAsync(id, ct);
        return Success("Evolução excluída.");
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] DocumentInput input, CancellationToken ct)
    {
        var file = input.File!;
        await using var stream = file.OpenReadStream();
        await documents.UploadAsync(
            input.ClientId,
            input.Category,
            input.Description,
            new DocumentUpload(file.FileName, file.ContentType, file.Length, stream),
            ct);
        return Success("Documento anexado.");
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id, bool inline, CancellationToken ct)
    {
        try
        {
            var file = await documents.OpenAsync(id, ct);
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return inline
                ? File(file.Content, file.Document.ContentType)
                : File(file.Content, file.Document.ContentType, file.Document.FileName);
        }
        catch (BusinessException)
        {
            return NotFound();
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDocument(long id, CancellationToken ct)
    {
        await documents.DeleteAsync(id, ct);
        return Success("Documento excluído.");
    }
}
