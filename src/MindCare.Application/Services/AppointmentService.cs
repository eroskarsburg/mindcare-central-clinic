using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Services;

/// <summary>Dados para criar ou alterar um agendamento e seu pagamento.</summary>
public sealed record AppointmentInput(
    int ClientId,
    AppointmentModality Modality,
    DateTime ScheduledAt,
    string? Observation,
    decimal Price,
    decimal PaidPrice);

public interface IAppointmentService
{
    Task<IReadOnlyList<Appointment>> ListAsync(CancellationToken ct = default);
    Task<int> CreateAsync(AppointmentInput input, CancellationToken ct = default);
    Task UpdateAsync(int id, AppointmentInput input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

internal sealed class AppointmentService(MindCareDbContext db, TimeProvider clock) : IAppointmentService
{
    public async Task<IReadOnlyList<Appointment>> ListAsync(CancellationToken ct = default) =>
        await db.Appointments.AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Payment)
            .OrderByDescending(a => a.ScheduledAt)
            .ToListAsync(ct);

    public async Task<int> CreateAsync(AppointmentInput input, CancellationToken ct = default)
    {
        var appointment = new Appointment { Payment = new Payment() };
        await ApplyAsync(appointment, input, ct);
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
        return appointment.Id;
    }

    public async Task UpdateAsync(int id, AppointmentInput input, CancellationToken ct = default)
    {
        var appointment = await db.Appointments.Include(a => a.Payment).FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new BusinessException("Agendamento não encontrado.");

        // Consultas importadas de versões antigas podem não ter pagamento associado.
        appointment.Payment ??= new Payment();
        await ApplyAsync(appointment, input, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default) =>
        await db.Appointments.Where(a => a.Id == id).ExecuteDeleteAsync(ct);

    private async Task ApplyAsync(Appointment target, AppointmentInput input, CancellationToken ct)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == input.ClientId, ct))
        {
            throw new BusinessException("Selecione um paciente válido.");
        }

        if (input.PaidPrice > input.Price)
        {
            throw new BusinessException("O valor pago não pode ser maior que o valor da consulta.");
        }

        target.ClientId = input.ClientId;
        target.Modality = input.Modality;
        target.ScheduledAt = input.ScheduledAt;
        target.Observation = string.IsNullOrWhiteSpace(input.Observation) ? null : input.Observation.Trim();

        var payment = target.Payment!;
        if (payment.PaidPrice != input.PaidPrice)
        {
            payment.PaidDate = null; // novo valor pago → data do pagamento passa a ser hoje
        }

        payment.Price = input.Price;
        payment.PaidPrice = input.PaidPrice;
        payment.Reconcile(clock.GetLocalNow().DateTime);
    }
}
