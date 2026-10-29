using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default);
    Task RegisterAsync(int id, decimal paidPrice, DateTime? paidDate, CancellationToken ct = default);
}

internal sealed class PaymentService(MindCareDbContext db, TimeProvider clock, IAccessScope? scope = null) : IPaymentService
{
    public async Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default)
    {
        var payments = db.Payments.AsNoTracking()
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(p => p.Appointment!).ThenInclude(a => a.Professional)
            .AsQueryable();

        if (await RestrictedAsync(ct) is { } own)
        {
            payments = payments.Where(p => p.Appointment!.ProfessionalId == own);
        }

        return await payments.OrderByDescending(p => p.Appointment!.ScheduledAt).ToListAsync(ct);
    }

    public async Task RegisterAsync(int id, decimal paidPrice, DateTime? paidDate, CancellationToken ct = default)
    {
        var payment = await db.Payments.Include(p => p.Appointment).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new BusinessException("Pagamento não encontrado.");

        if (await RestrictedAsync(ct) is { } own && payment.Appointment?.ProfessionalId != own)
        {
            throw new BusinessException("Você só pode registrar pagamentos dos seus próprios atendimentos.");
        }

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

    private Task<int?> RestrictedAsync(CancellationToken ct) =>
        scope?.RestrictedProfessionalIdAsync(ct) ?? Task.FromResult<int?>(null);
}
