using Microsoft.EntityFrameworkCore;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed record Birthday(int ClientId, string Name, DateTime Date, int Age);

public sealed record DashboardData(
    DashboardSummary Summary,
    IReadOnlyList<Appointment> Today,
    IReadOnlyList<Appointment> Upcoming,
    IReadOnlyList<Payment> OpenPayments,
    IReadOnlyList<Birthday> Birthdays);

public interface IDashboardService
{
    Task<DashboardData> GetAsync(CancellationToken ct = default);
}

internal sealed class DashboardService(MindCareDbContext db, TimeProvider clock, IAccessScope? scope = null) : IDashboardService
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
        var last30 = today.AddDays(-30);

        var own = scope is null ? null : await scope.RestrictedProfessionalIdAsync(ct);
        var appointments = db.Appointments.AsNoTracking()
            .Where(a => own == null || a.ProfessionalId == own);
        var active = appointments.Where(a => a.Status != AppointmentStatus.Cancelada);
        var payments = db.Payments.AsNoTracking()
            .Where(p => own == null || p.Appointment!.ProfessionalId == own);

        // Faltas sobre as consultas que deveriam ter acontecido (realizadas + faltas) nos últimos 30 dias.
        var attendance = await appointments
            .Where(a => a.ScheduledAt >= last30 && a.ScheduledAt < now
                        && (a.Status == AppointmentStatus.Realizada || a.Status == AppointmentStatus.Faltou))
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var noShows = attendance.Where(a => a.Status == AppointmentStatus.Faltou).Sum(a => a.Count);
        var expected = attendance.Sum(a => a.Count);

        var summary = new DashboardSummary(
            AppointmentsToday: await active.CountAsync(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow, ct),
            AppointmentsNext7Days: await active.CountAsync(a => a.ScheduledAt >= now && a.ScheduledAt < nextWeek, ct),
            TotalClients: await db.Clients.CountAsync(c => c.AnonymizedAt == null, ct),
            OutstandingAmount: await payments
                .Where(p => p.Status != PaymentStatus.Confirmado && p.Appointment!.Status != AppointmentStatus.Cancelada)
                .SumAsync(p => p.Price - p.PaidPrice, ct),
            ReceivedThisMonth: await payments
                .Where(p => p.PaidDate >= monthStart && p.PaidDate < nextMonth)
                .SumAsync(p => p.PaidPrice, ct),
            NoShowRate: expected == 0 ? null : (decimal)noShows / expected,
            PendingConfirmations: await active.CountAsync(
                a => a.Status == AppointmentStatus.Agendada && a.ScheduledAt >= now && a.ScheduledAt < tomorrow.AddDays(1), ct));

        var todayList = await active
            .Include(a => a.Client).Include(a => a.Professional)
            .Where(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow)
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync(ct);

        var upcoming = await active
            .Include(a => a.Client).Include(a => a.Professional)
            .Where(a => a.ScheduledAt >= tomorrow)
            .OrderBy(a => a.ScheduledAt)
            .Take(ListSize)
            .ToListAsync(ct);

        var openPayments = await payments
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Where(p => p.Status != PaymentStatus.Confirmado && p.Appointment!.Status != AppointmentStatus.Cancelada
                        && p.Appointment.ScheduledAt < tomorrow)
            .OrderBy(p => p.Appointment!.ScheduledAt)
            .Take(ListSize)
            .ToListAsync(ct);

        // Aniversariantes dos próximos 7 dias entre os pacientes em acompanhamento.
        var withBirthday = await db.Clients.AsNoTracking()
            .Where(c => c.BirthDate != null && c.AnonymizedAt == null && c.Status == ClientStatus.Ativo
                        && (own == null || c.ResponsibleProfessionalId == own))
            .Select(c => new { c.Id, c.Name, BirthDate = c.BirthDate!.Value })
            .ToListAsync(ct);

        var birthdays = withBirthday
            .Select(c =>
            {
                var next = NextBirthday(c.BirthDate, today);
                return new Birthday(c.Id, c.Name, next, next.Year - c.BirthDate.Year);
            })
            .Where(b => b.Date < today.AddDays(7))
            .OrderBy(b => b.Date)
            .ToList();

        return new DashboardData(summary, todayList, upcoming, openPayments, birthdays);
    }

    private static DateTime NextBirthday(DateTime birth, DateTime today)
    {
        DateTime On(int year) => new(year, birth.Month, Math.Min(birth.Day, DateTime.DaysInMonth(year, birth.Month)));
        var thisYear = On(today.Year);
        return thisYear >= today ? thisYear : On(today.Year + 1);
    }
}
