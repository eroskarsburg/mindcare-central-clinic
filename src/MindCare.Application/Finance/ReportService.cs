using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Finance;

public sealed record MonthPoint(DateTime Month, decimal Billed, decimal Received);

public sealed record ProfessionalPerformance(
    int ProfessionalId,
    string Name,
    int SessionsDone,
    int NoShows,
    decimal Billed,
    decimal Received,
    decimal CommissionPercent,
    decimal Commission,
    int BookedMinutes,
    int AvailableMinutes)
{
    public decimal? Occupancy => AvailableMinutes == 0 ? null : (decimal)BookedMinutes / AvailableMinutes;
}

public sealed record MethodTotal(PaymentMethod Method, decimal Amount, int Count);

public sealed record OverdueItem(int PaymentId, int ClientId, string ClientName, string? ProfessionalName, DateTime ScheduledAt, decimal Outstanding, int DaysOverdue);

public sealed record EntryRow(DateTime PaidAt, string ClientName, string Description, string? ProfessionalName, PaymentMethod Method, decimal Amount);

public sealed record FinancialReport(
    DateTime From,
    DateTime To,
    decimal Billed,
    decimal Received,
    decimal Outstanding,
    decimal? NoShowRate,
    decimal? Occupancy,
    IReadOnlyList<MonthPoint> Months,
    IReadOnlyList<ProfessionalPerformance> Professionals,
    IReadOnlyList<MethodTotal> Methods,
    IReadOnlyList<OverdueItem> Overdue,
    IReadOnlyList<EntryRow> Entries);

public interface IReportService
{
    /// <summary>Indicadores financeiros e de agenda do período [from, to).</summary>
    /// <param name="from">Início do período (inclusive).</param>
    /// <param name="to">Fim do período (exclusive).</param>
    /// <param name="professionalId">Filtra um profissional (ignorado para quem tem acesso restrito).</param>
    /// <param name="ct">Cancelamento.</param>
    Task<FinancialReport> GetAsync(DateTime from, DateTime to, int? professionalId = null, CancellationToken ct = default);

    byte[] ToExcel(FinancialReport report);
}

internal sealed class ReportService(MindCareDbContext db, IAccessScope scope, TimeProvider clock) : IReportService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<FinancialReport> GetAsync(DateTime from, DateTime to, int? professionalId = null, CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        var own = await scope.RestrictedProfessionalIdAsync(ct) ?? professionalId;

        // Série dos últimos 12 meses até o fim do período (para o gráfico).
        var seriesStart = new DateTime(to.AddDays(-1).Year, to.AddDays(-1).Month, 1).AddMonths(-11);
        var rangeStart = seriesStart < from ? seriesStart : from;

        var appointments = await db.Appointments.AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Professional)
            .Include(a => a.Payment)
            .Where(a => a.ScheduledAt >= rangeStart && a.ScheduledAt < to && (own == null || a.ProfessionalId == own))
            .ToListAsync(ct);

        var entries = await db.PaymentEntries.AsNoTracking()
            .Include(e => e.Payment!).ThenInclude(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(e => e.Payment!).ThenInclude(p => p.Appointment!).ThenInclude(a => a.Professional)
            .Include(e => e.Package!).ThenInclude(p => p.Client)
            .Where(e => e.PaidAt >= rangeStart && e.PaidAt < to)
            .ToListAsync(ct);
        if (own is not null)
        {
            entries = [.. entries.Where(e => e.Payment?.Appointment?.ProfessionalId == own)];
        }

        var packages = own is null
            ? await db.Packages.AsNoTracking().Where(p => p.PurchasedAt >= rangeStart && p.PurchasedAt < to).ToListAsync(ct)
            : [];

        bool InPeriod(DateTime d) => d >= from && d < to;
        var periodAppointments = appointments.Where(a => InPeriod(a.ScheduledAt) && a.Status != AppointmentStatus.Cancelada).ToList();
        var periodEntries = entries.Where(e => InPeriod(e.PaidAt)).ToList();

        // Faturado: consultas do período (avulsas) + pacotes vendidos no período.
        decimal BilledIn(DateTime start, DateTime end) =>
            appointments.Where(a => a.ScheduledAt >= start && a.ScheduledAt < end && a.Status != AppointmentStatus.Cancelada)
                .Sum(a => a.Payment?.Price ?? 0)
            + packages.Where(p => p.PurchasedAt >= start && p.PurchasedAt < end).Sum(p => p.Price);

        var months = Enumerable.Range(0, 12)
            .Select(i => seriesStart.AddMonths(i))
            .Select(m => new MonthPoint(m, BilledIn(m, m.AddMonths(1)), entries.Where(e => e.PaidAt >= m && e.PaidAt < m.AddMonths(1)).Sum(e => e.Amount)))
            .ToList();

        var attendance = periodAppointments.Where(a => a.ScheduledAt < now && a.Status is AppointmentStatus.Realizada or AppointmentStatus.Faltou).ToList();

        var professionals = await BuildProfessionalsAsync(from, to, own, periodAppointments, periodEntries, ct);
        var available = professionals.Sum(p => p.AvailableMinutes);

        var overdue = await db.Payments.AsNoTracking()
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(p => p.Appointment!).ThenInclude(a => a.Professional)
            .Where(p => p.Status != PaymentStatus.Confirmado
                        && p.Appointment!.ScheduledAt < now
                        && p.Appointment.Status != AppointmentStatus.Cancelada
                        && (own == null || p.Appointment.ProfessionalId == own))
            .ToListAsync(ct);

        return new FinancialReport(
            from,
            to,
            BilledIn(from, to),
            periodEntries.Sum(e => e.Amount),
            overdue.Sum(p => p.Outstanding),
            attendance.Count == 0 ? null : (decimal)attendance.Count(a => a.Status == AppointmentStatus.Faltou) / attendance.Count,
            available == 0 ? null : (decimal)professionals.Sum(p => p.BookedMinutes) / available,
            months,
            professionals,
            [.. periodEntries.GroupBy(e => e.Method)
                .Select(g => new MethodTotal(g.Key, g.Sum(e => e.Amount), g.Count()))
                .OrderByDescending(m => m.Amount)],
            [.. overdue
                .Select(p => new OverdueItem(p.Id, p.Appointment!.ClientId, p.Appointment.Client?.Name ?? "—", p.Appointment.Professional?.Name,
                    p.Appointment.ScheduledAt, p.Outstanding, (int)(now.Date - p.Appointment.ScheduledAt.Date).TotalDays))
                .OrderByDescending(o => o.DaysOverdue)],
            [.. periodEntries
                .OrderBy(e => e.PaidAt)
                .Select(e => e.Payment?.Appointment is { } a
                    ? new EntryRow(e.PaidAt, a.Client?.Name ?? "—", $"Sessão {a.ScheduledAt:dd/MM/yyyy}", a.Professional?.Name, e.Method, e.Amount)
                    : new EntryRow(e.PaidAt, e.Package?.Client?.Name ?? "—", $"Pacote {e.Package?.Name}", null, e.Method, e.Amount))]);
    }

    private async Task<List<ProfessionalPerformance>> BuildProfessionalsAsync(
        DateTime from, DateTime to, int? own, List<Appointment> periodAppointments, List<PaymentEntry> periodEntries, CancellationToken ct)
    {
        var professionals = await db.Professionals.AsNoTracking()
            .Include(p => p.Availability)
            .Where(p => own == null || p.Id == own)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var blocks = await db.ScheduleBlocks.AsNoTracking().Where(b => b.StartsAt < to && b.EndsAt > from).ToListAsync(ct);

        return [.. professionals.Select(p =>
        {
            var mine = periodAppointments.Where(a => a.ProfessionalId == p.Id).ToList();
            var received = periodEntries.Where(e => e.Payment?.Appointment?.ProfessionalId == p.Id).Sum(e => e.Amount);
            return new ProfessionalPerformance(
                p.Id,
                p.Name,
                mine.Count(a => a.Status == AppointmentStatus.Realizada),
                mine.Count(a => a.Status == AppointmentStatus.Faltou),
                mine.Sum(a => a.Payment?.Price ?? 0),
                received,
                p.CommissionPercent,
                decimal.Round(received * p.CommissionPercent / 100m, 2),
                mine.Sum(a => a.DurationMinutes),
                AvailableMinutes(p, from, to, blocks));
        })];
    }

    /// <summary>Minutos de atendimento disponíveis no período, descontando dias bloqueados.</summary>
    private static int AvailableMinutes(Professional p, DateTime from, DateTime to, List<ScheduleBlock> blocks)
    {
        var minutes = 0;
        for (var day = from.Date; day < to; day = day.AddDays(1))
        {
            foreach (var slot in p.Availability.Where(s => s.DayOfWeek == day.DayOfWeek))
            {
                var start = day + slot.StartTime.ToTimeSpan();
                var end = day + slot.EndTime.ToTimeSpan();
                if (!blocks.Any(b => (b.ProfessionalId == null || b.ProfessionalId == p.Id) && b.Overlaps(start, end)))
                {
                    minutes += (int)(end - start).TotalMinutes;
                }
            }
        }

        return minutes;
    }

    public byte[] ToExcel(FinancialReport report)
    {
        using var workbook = new XLWorkbook();
        const string Money = "R$ #,##0.00";
        var period = $"{report.From:dd/MM/yyyy} a {report.To.AddDays(-1):dd/MM/yyyy}";

        var summary = workbook.AddWorksheet("Resumo");
        summary.Cell(1, 1).Value = "Relatório financeiro";
        summary.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        summary.Cell(2, 1).Value = $"Período: {period}";
        (string Label, XLCellValue Value, string? Format)[] rows =
        [
            ("Faturado", report.Billed, Money),
            ("Recebido", report.Received, Money),
            ("Em aberto (vencido)", report.Outstanding, Money),
            ("Taxa de faltas", report.NoShowRate is { } r ? (double)r : "—", "0.0%"),
            ("Ocupação da agenda", report.Occupancy is { } o ? (double)o : "—", "0.0%"),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            summary.Cell(4 + i, 1).Value = rows[i].Label;
            var cell = summary.Cell(4 + i, 2);
            cell.Value = rows[i].Value;
            if (rows[i].Format is { } format)
            {
                cell.Style.NumberFormat.Format = format;
            }
        }

        summary.Cell(11, 1).Value = "Mês";
        summary.Cell(11, 2).Value = "Faturado";
        summary.Cell(11, 3).Value = "Recebido";
        for (var i = 0; i < report.Months.Count; i++)
        {
            var m = report.Months[i];
            summary.Cell(12 + i, 1).Value = m.Month.ToString("MMM/yyyy", PtBr);
            summary.Cell(12 + i, 2).Value = m.Billed;
            summary.Cell(12 + i, 3).Value = m.Received;
        }

        summary.Range(12, 2, 11 + report.Months.Count, 3).Style.NumberFormat.Format = Money;
        summary.Row(11).Style.Font.SetBold();
        summary.Columns().AdjustToContents();

        AddTable(workbook, "Por profissional", ["Profissional", "Sessões realizadas", "Faltas", "Faturado", "Recebido", "Repasse %", "Repasse", "Ocupação"],
            report.Professionals.Select(p => new XLCellValue[]
            {
                p.Name, p.SessionsDone, p.NoShows, p.Billed, p.Received, (double)(p.CommissionPercent / 100m), p.Commission,
                p.Occupancy is { } o ? (double)o : "—",
            }),
            new() { [4] = Money, [5] = Money, [6] = "0.0%", [7] = Money, [8] = "0.0%" });

        AddTable(workbook, "Recebimentos", ["Data", "Paciente", "Descrição", "Profissional", "Forma", "Valor"],
            report.Entries.Select(e => new XLCellValue[]
            {
                e.PaidAt, e.ClientName, e.Description, e.ProfessionalName ?? "—", e.Method.DisplayName(), e.Amount,
            }),
            new() { [1] = "dd/mm/yyyy", [6] = Money });

        AddTable(workbook, "Inadimplência", ["Sessão", "Paciente", "Profissional", "Dias em atraso", "Em aberto"],
            report.Overdue.Select(o => new XLCellValue[]
            {
                o.ScheduledAt, o.ClientName, o.ProfessionalName ?? "—", o.DaysOverdue, o.Outstanding,
            }),
            new() { [1] = "dd/mm/yyyy hh:mm", [5] = Money });

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddTable(XLWorkbook workbook, string name, string[] headers, IEnumerable<XLCellValue[]> rows, Dictionary<int, string> formats)
    {
        var sheet = workbook.AddWorksheet(name);
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
            {
                sheet.Cell(r, c + 1).Value = row[c];
            }

            r++;
        }

        if (r > 2)
        {
            var table = sheet.Range(1, 1, r - 1, headers.Length).CreateTable(name.Replace(" ", string.Empty, StringComparison.Ordinal));
            table.Theme = XLTableTheme.TableStyleLight1;
            foreach (var (column, format) in formats)
            {
                sheet.Range(2, column, r - 1, column).Style.NumberFormat.Format = format;
            }
        }
        else
        {
            sheet.Row(1).Style.Font.SetBold();
            sheet.Cell(2, 1).Value = "Sem registros no período.";
        }

        sheet.Columns().AdjustToContents();
    }
}
