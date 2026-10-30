using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Domain;
using MindCare.Application.Security;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class AppointmentController(
    IAppointmentService appointments,
    IClientService clients,
    IProfessionalService professionals,
    IScheduleService schedule,
    IPaymentService payments,
    IAccessScope scope) : AppController
{
    private static readonly string[] BlockClasses = ["ev-block"];

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var allClients = await clients.ListAsync(ct);
        return View(new AppointmentsPageViewModel(
            await appointments.ListAsync(ct: ct),
            [.. allClients.Where(c => !c.IsAnonymized)],
            await professionals.ListAsync(ct),
            await scope.RestrictedProfessionalIdAsync(ct),
            await payments.ListServiceTypesAsync(onlyActive: true, ct)));
    }

    /// <summary>Eventos do calendário (FullCalendar) no intervalo visível.</summary>
    [HttpGet]
    public async Task<IActionResult> Events(DateTime start, DateTime end, int? professionalId, CancellationToken ct)
    {
        var items = await appointments.ListAsync(new AppointmentQuery(start, end, professionalId), ct);
        var blocks = await schedule.ListBlocksAsync(start, end, professionalId, ct);

        var events = items.Select(a => (object)new
        {
            id = a.Id.ToString(),
            title = a.Client?.Name,
            start = a.ScheduledAt,
            end = a.EndsAt,
            classNames = new[] { "ev", $"ev-{a.Status.ToString().ToLowerInvariant()}" },
            editable = a.Status is AppointmentStatus.Agendada or AppointmentStatus.Confirmada,
            extendedProps = new
            {
                status = a.Status.ToString(),
                modality = a.Modality.ToString(),
                professional = a.Professional?.Name,
                isSeries = a.SeriesId is not null,
                meetingUrl = a.MeetingUrl,
                paymentStatus = a.Payment?.Status.ToString(),
                item = ToFormItem(a),
            },
        }).Concat(blocks.Select(b => (object)new
        {
            id = $"block-{b.Id}",
            title = b.Professional is null ? b.Reason : $"{b.Reason} · {b.Professional.Name}",
            start = b.StartsAt,
            end = b.EndsAt,
            display = "background",
            classNames = BlockClasses,
        }));

        return Json(events);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentFormInput input, CancellationToken ct)
    {
        var result = await appointments.CreateAsync(input.ToInput(), ct);
        return Success(result.Describe("Consulta agendada.", "consultas agendadas."), result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentFormInput input, CancellationToken ct)
    {
        var result = await appointments.UpdateAsync(id, input.ToInput(), input.Scope, ct);
        return Success(result.Describe("Consulta atualizada.", "consultas atualizadas."), result);
    }

    [HttpPut]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleInput input, CancellationToken ct)
    {
        var result = await appointments.RescheduleAsync(id, input.Start!.Value, input.DurationMinutes, ct);
        return Success(result.Describe("Consulta remarcada.", "consultas remarcadas."), result);
    }

    [HttpPut]
    public async Task<IActionResult> Status(int id, [FromBody] StatusInput input, CancellationToken ct)
    {
        var result = await appointments.SetStatusAsync(id, input.Status, input.Reason, input.Scope, ct);
        var (singular, plural) = input.Status switch
        {
            AppointmentStatus.Confirmada => ("Consulta confirmada.", "consultas confirmadas."),
            AppointmentStatus.Realizada => ("Presença registrada.", "consultas registradas como realizadas."),
            AppointmentStatus.Faltou => ("Falta registrada.", "faltas registradas."),
            AppointmentStatus.Cancelada => ("Consulta cancelada.", "consultas canceladas."),
            _ => ("Consulta reaberta.", "consultas reabertas."),
        };
        return Success(result.Describe(singular, plural), result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await appointments.DeleteAsync(id, ct);
        return Success("Consulta excluída.");
    }

    /// <summary>Dados usados para preencher o formulário de edição (lista e calendário).</summary>
    public static object ToFormItem(Appointment a) => new
    {
        a.Id,
        a.ClientId,
        a.ProfessionalId,
        a.Modality,
        a.ScheduledAt,
        a.DurationMinutes,
        a.ServiceTypeId,
        UsesPackage = a.PackageId is not null,
        a.Status,
        a.CancellationReason,
        a.Observation,
        a.MeetingUrl,
        IsSeries = a.SeriesId is not null,
        ClientName = a.Client?.Name,
        Price = a.Payment?.Price ?? 0,
        PaidPrice = a.Payment is { PaidPrice: > 0 } p ? p.PaidPrice : (decimal?)null,
    };
}
