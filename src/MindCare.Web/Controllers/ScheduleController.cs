using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Security;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

/// <summary>Horários de atendimento e bloqueios de agenda. O profissional gerencia a própria; o admin, a de todos.</summary>
public sealed class ScheduleController(
    IScheduleService schedule,
    IProfessionalService professionals,
    IAccessScope scope) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id, CancellationToken ct)
    {
        var own = await scope.RestrictedProfessionalIdAsync(ct);
        var all = await professionals.ListAsync(ct);
        var professionalId = own ?? id ?? (all.Count > 0 ? all[0].Id : null);

        if (professionalId is null)
        {
            return View("Empty");
        }

        return View(new SchedulePageViewModel(await schedule.GetAsync(professionalId.Value, ct), all, own is null));
    }

    [HttpPut]
    public async Task<IActionResult> Availability(int id, [FromBody] AvailabilityInput input, CancellationToken ct)
    {
        await schedule.SaveAvailabilityAsync(id, input.ToSlots(), ct);
        return Success("Horários de atendimento salvos.");
    }

    [HttpPost]
    public async Task<IActionResult> Block([FromBody] BlockFormInput input, CancellationToken ct)
    {
        await schedule.AddBlockAsync(input.ToInput(), ct);
        return Success("Período bloqueado na agenda.");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBlock(int id, CancellationToken ct)
    {
        await schedule.DeleteBlockAsync(id, ct);
        return Success("Bloqueio removido.");
    }
}
