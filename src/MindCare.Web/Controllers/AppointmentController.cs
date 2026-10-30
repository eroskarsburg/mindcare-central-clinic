using Microsoft.AspNetCore.Mvc;
using MindCare.Application.Services;
using MindCare.Web.Infrastructure;
using MindCare.Web.Models;

namespace MindCare.Web.Controllers;

public sealed class AppointmentController(IAppointmentService appointments, IClientService clients) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new AppointmentsPageViewModel(await appointments.ListAsync(ct), await clients.ListAsync(ct)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentFormInput input, CancellationToken ct)
    {
        await appointments.CreateAsync(input.ToInput(), ct);
        return Success("Consulta agendada.");
    }

    [HttpPut]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentFormInput input, CancellationToken ct)
    {
        await appointments.UpdateAsync(id, input.ToInput(), ct);
        return Success("Consulta atualizada.");
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await appointments.DeleteAsync(id, ct);
        return Success("Consulta excluída.");
    }
}
