using Microsoft.EntityFrameworkCore;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Services;

public sealed record DashboardData(
    DashboardSummary Summary,
    IReadOnlyList<Appointment> Upcoming,
    IReadOnlyList<Payment> OpenPayments);

public interface IDashboardService
{
    Task<DashboardData> GetAsync(CancellationToken ct = default);
}

internal sealed class DashboardService(MindCareDbContext db, TimeProvider clock) : IDashboardService
{
    private const int ListSize = 6;

    public async Task<DashboardData> GetAsync(CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var nextWeek = today.AddDays(8);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var summary = new DashboardSummary(
            AppointmentsToday: await db.Appointments.CountAsync(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow, ct),
            AppointmentsNext7Days: await db.Appointments.CountAsync(a => a.ScheduledAt >= now && a.ScheduledAt < nextWeek, ct),
            TotalClients: await db.Clients.CountAsync(ct),
            OutstandingAmount: await db.Payments
                .Where(p => p.Status != PaymentStatus.Confirmado)
                .SumAsync(p => p.Price - p.PaidPrice, ct),
            ReceivedThisMonth: await db.Payments
                .Where(p => p.PaidDate >= monthStart && p.PaidDate < nextMonth)
                .SumAsync(p => p.PaidPrice, ct));

        var upcoming = await db.Appointments.AsNoTracking()
            .Include(a => a.Client)
            .Where(a => a.ScheduledAt >= now)
            .OrderBy(a => a.ScheduledAt)
            .Take(ListSize)
            .ToListAsync(ct);

        var openPayments = await db.Payments.AsNoTracking()
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Where(p => p.Status != PaymentStatus.Confirmado)
            .OrderBy(p => p.Appointment!.ScheduledAt)
            .Take(ListSize)
            .ToListAsync(ct);

        return new DashboardData(summary, upcoming, openPayments);
    }
}
