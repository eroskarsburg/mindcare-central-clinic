using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Services;

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default);
    Task RegisterAsync(int id, decimal paidPrice, DateTime? paidDate, CancellationToken ct = default);
}

internal sealed class PaymentService(MindCareDbContext db, TimeProvider clock) : IPaymentService
{
    public async Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default) =>
        await db.Payments.AsNoTracking()
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .OrderByDescending(p => p.Appointment!.ScheduledAt)
            .ToListAsync(ct);

    public async Task RegisterAsync(int id, decimal paidPrice, DateTime? paidDate, CancellationToken ct = default)
    {
        var payment = await db.Payments.FindAsync([id], ct)
            ?? throw new BusinessException("Pagamento não encontrado.");

        if (paidPrice < 0 || paidPrice > payment.Price)
        {
            throw new BusinessException("O valor pago deve estar entre zero e o valor da consulta.");
        }

        if (paidDate is not null || paidPrice != payment.PaidPrice)
        {
            payment.PaidDate = paidDate;
        }

        payment.PaidPrice = paidPrice;
        payment.Reconcile(clock.GetLocalNow().DateTime);
        await db.SaveChangesAsync(ct);
    }
}
