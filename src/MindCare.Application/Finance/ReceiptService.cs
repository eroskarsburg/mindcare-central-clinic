using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MindCare.Application.Finance;

/// <summary>Dados da clínica impressos nos recibos (seção "Clinic").</summary>
public sealed class ClinicLegalOptions
{
    public const string Section = "Clinic";

    public string Name { get; set; } = "MindCare";
    public string? LegalName { get; set; }
    public string? Cnpj { get; set; }
    public string? Address { get; set; }
    public string City { get; set; } = "Porto Alegre";
}

public sealed record ReceiptFile(string FileName, byte[] Content);

public interface IReceiptService
{
    /// <summary>Recibo dos valores recebidos de uma consulta.</summary>
    Task<ReceiptFile> ForPaymentAsync(int paymentId, CancellationToken ct = default);

    /// <summary>Recibo mensal consolidado do paciente (útil para a declaração de IR).</summary>
    Task<ReceiptFile> MonthlyAsync(int clientId, int year, int month, CancellationToken ct = default);
}

internal sealed class ReceiptService(
    MindCareDbContext db,
    IAccessScope scope,
    IOptions<ClinicLegalOptions> clinic,
    TimeProvider clock) : IReceiptService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    static ReceiptService() => QuestPDF.Settings.License = LicenseType.Community;

    public async Task<ReceiptFile> ForPaymentAsync(int paymentId, CancellationToken ct = default)
    {
        var payment = await db.Payments.AsNoTracking()
            .Include(p => p.Entries)
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(p => p.Appointment!).ThenInclude(a => a.Professional)
            .Include(p => p.Appointment!).ThenInclude(a => a.ServiceType)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new BusinessException("Pagamento não encontrado.");

        var appointment = payment.Appointment!;
        await EnsureAccessAsync(appointment.ProfessionalId, ct);
        if (payment.Entries.Count == 0)
        {
            throw new BusinessException("Não há valores recebidos para esta consulta.");
        }

        var lines = payment.Entries.OrderBy(e => e.PaidAt)
            .Select(e => new ReceiptLine(appointment.ScheduledAt, Describe(appointment), e.PaidAt, e.Method, e.Amount))
            .ToList();

        var number = $"{appointment.ScheduledAt:yyyyMM}-{payment.Id:D5}";
        var pdf = Render(number, appointment.Client!, appointment.Professional, lines,
            $"referente à sessão de {appointment.ScheduledAt.ToString("dd/MM/yyyy", PtBr)}");
        return new ReceiptFile($"recibo-{number}.pdf", pdf);
    }

    public async Task<ReceiptFile> MonthlyAsync(int clientId, int year, int month, CancellationToken ct = default)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, ct)
            ?? throw new BusinessException("Paciente não encontrado.");
        var from = new DateTime(year, month, 1);
        var to = from.AddMonths(1);
        var own = await scope.RestrictedProfessionalIdAsync(ct);

        var entries = await db.PaymentEntries.AsNoTracking()
            .Include(e => e.Payment!).ThenInclude(p => p.Appointment!).ThenInclude(a => a.Professional)
            .Include(e => e.Payment!).ThenInclude(p => p.Appointment!).ThenInclude(a => a.ServiceType)
            .Include(e => e.Package)
            .Where(e => e.PaidAt >= from && e.PaidAt < to
                        && ((e.Payment != null && e.Payment.Appointment!.ClientId == clientId)
                            || (e.Package != null && e.Package.ClientId == clientId)))
            .OrderBy(e => e.PaidAt)
            .ToListAsync(ct);

        if (own is not null)
        {
            entries = [.. entries.Where(e => e.Payment?.Appointment?.ProfessionalId == own)];
        }

        if (entries.Count == 0)
        {
            throw new BusinessException("Não há valores recebidos deste paciente no mês selecionado.");
        }

        var lines = entries.Select(e => e.Payment?.Appointment is { } a
                ? new ReceiptLine(a.ScheduledAt, Describe(a), e.PaidAt, e.Method, e.Amount)
                : new ReceiptLine(null, $"Pacote: {e.Package!.Name} ({e.Package.TotalSessions} sessões)", e.PaidAt, e.Method, e.Amount))
            .ToList();

        // Um único profissional no mês: ele assina; vários: assina a clínica.
        var professionals = entries.Select(e => e.Payment?.Appointment?.Professional).Where(p => p is not null).DistinctBy(p => p!.Id).ToList();
        var signer = professionals.Count == 1 ? professionals[0] : null;

        var monthName = from.ToString("MMMM 'de' yyyy", PtBr);
        var number = $"{year}{month:D2}-C{clientId:D5}";
        var pdf = Render(number, client, signer, lines, $"referente aos atendimentos de {monthName}");
        return new ReceiptFile($"recibo-{client.Name.Split(' ')[0].ToLowerInvariant()}-{year}-{month:D2}.pdf", pdf);
    }

    private async Task EnsureAccessAsync(int? professionalId, CancellationToken ct)
    {
        if (await scope.RestrictedProfessionalIdAsync(ct) is { } own && own != professionalId)
        {
            throw new BusinessException("Você só pode emitir recibos dos seus próprios atendimentos.");
        }
    }

    private static string Describe(Appointment a) =>
        a.ServiceType?.Name ?? (a.Modality == AppointmentModality.Teleconsulta ? "Sessão (teleconsulta)" : "Sessão");

    private sealed record ReceiptLine(DateTime? SessionAt, string Description, DateTime PaidAt, PaymentMethod Method, decimal Amount);

    private byte[] Render(string number, Client client, Professional? professional, IReadOnlyList<ReceiptLine> lines, string reference)
    {
        var o = clinic.Value;
        var total = lines.Sum(l => l.Amount);
        var today = clock.GetLocalNow().DateTime;
        const string Primary = "#7D6049";
        const string Muted = "#7B6E63";
        const string Border = "#E8DDD0";

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(42);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor("#2E2723"));

            page.Header().BorderBottom(1).BorderColor(Border).PaddingBottom(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(o.LegalName ?? o.Name).FontSize(16).Bold().FontColor("#2F3E46");
                    if (!string.IsNullOrWhiteSpace(o.Cnpj))
                    {
                        col.Item().Text($"CNPJ {o.Cnpj}").FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(o.Address))
                    {
                        col.Item().Text(o.Address).FontColor(Muted);
                    }
                });
                row.ConstantItem(170).AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text("RECIBO").FontSize(18).Bold().FontColor(Primary);
                    col.Item().AlignRight().Text($"Nº {number}").FontColor(Muted);
                    col.Item().AlignRight().Text(total.ToString("C", PtBr)).FontSize(14).Bold();
                });
            });

            page.Content().PaddingVertical(22).Column(col =>
            {
                col.Spacing(14);

                col.Item().Text(text =>
                {
                    text.Span("Recebi de ");
                    text.Span(client.Name).Bold();
                    if (Cpf.IsValid(client.Cpf))
                    {
                        text.Span($", CPF {Cpf.Format(client.Cpf)}");
                    }

                    text.Span(", a importância de ");
                    text.Span($"{total.ToString("C", PtBr)} ({MoneyInWords.Format(total)})").Bold();
                    text.Span($", {reference}, conforme discriminado abaixo.");
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(80);
                        c.RelativeColumn();
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                    });

                    static IContainer Head(IContainer cell) =>
                        cell.BorderBottom(1).BorderColor("#E8DDD0").PaddingVertical(6).DefaultTextStyle(t => t.Bold().FontSize(9).FontColor("#7B6E63"));

                    table.Header(h =>
                    {
                        h.Cell().Element(Head).Text("SESSÃO");
                        h.Cell().Element(Head).Text("DESCRIÇÃO");
                        h.Cell().Element(Head).Text("PAGO EM");
                        h.Cell().Element(Head).Text("FORMA");
                        h.Cell().Element(Head).AlignRight().Text("VALOR");
                    });

                    foreach (var line in lines)
                    {
                        IContainer Cell(IContainer cell) => cell.BorderBottom(1).BorderColor("#F1E8DE").PaddingVertical(6);
                        table.Cell().Element(Cell).Text(line.SessionAt?.ToString("dd/MM/yyyy", PtBr) ?? "—");
                        table.Cell().Element(Cell).Text(line.Description);
                        table.Cell().Element(Cell).Text(line.PaidAt.ToString("dd/MM/yyyy", PtBr));
                        table.Cell().Element(Cell).Text(line.Method.DisplayName());
                        table.Cell().Element(Cell).AlignRight().Text(line.Amount.ToString("C", PtBr));
                    }

                    table.Footer(f =>
                    {
                        f.Cell().ColumnSpan(4).PaddingTop(8).AlignRight().Text("Total").Bold();
                        f.Cell().PaddingTop(8).AlignRight().Text(total.ToString("C", PtBr)).Bold();
                    });
                });

                col.Item().PaddingTop(10).Text($"{o.City}, {today.ToString("dd 'de' MMMM 'de' yyyy", PtBr)}.");

                col.Item().PaddingTop(40).AlignCenter().Width(280).Column(sign =>
                {
                    sign.Item().BorderTop(1).BorderColor("#2E2723").PaddingTop(6).AlignCenter()
                        .Text(professional?.Name ?? o.LegalName ?? o.Name).Bold();
                    if (professional is not null)
                    {
                        var registry = string.IsNullOrWhiteSpace(professional.RegistrationNumber) ? professional.Speciality : $"{professional.Speciality} · {professional.RegistrationNumber}";
                        sign.Item().AlignCenter().Text(registry).FontColor(Muted);
                        if (Cpf.IsValid(professional.Cpf))
                        {
                            sign.Item().AlignCenter().Text($"CPF {Cpf.Format(professional.Cpf)}").FontColor(Muted);
                        }
                    }
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span($"Emitido por {o.Name} em {today.ToString("dd/MM/yyyy HH:mm", PtBr)}").FontSize(8).FontColor(Muted);
            });
        })).GeneratePdf();
    }
}
