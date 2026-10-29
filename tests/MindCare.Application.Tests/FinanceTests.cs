using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Finance;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public class MoneyInWordsTests
{
    [Theory]
    [InlineData(1, "um real")]
    [InlineData(0.01, "um centavo")]
    [InlineData(150, "cento e cinquenta reais")]
    [InlineData(101, "cento e um reais")]
    [InlineData(100, "cem reais")]
    [InlineData(1000, "mil reais")]
    [InlineData(1100, "mil e cem reais")]
    [InlineData(1020, "mil e vinte reais")]
    [InlineData(1250.30, "mil duzentos e cinquenta reais e trinta centavos")]
    [InlineData(21000, "vinte e um mil reais")]
    [InlineData(2000000, "dois milhões de reais")]
    [InlineData(1200000, "um milhão e duzentos mil reais")]
    public void Escreve_valores_por_extenso(double value, string expected) =>
        Assert.Equal(expected, MoneyInWords.Format((decimal)value));
}

public sealed class FinanceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 5, 4, 10, 0, 0);
    private readonly TestDatabase _database = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(Now, TimeSpan.Zero));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Recebimentos_em_parcelas_atualizam_status_e_saldo()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(s, Now.AddDays(-7)), Ct);
        var paymentId = await db.Payments.Select(p => p.Id).SingleAsync(Ct);
        var payments = Payments(db);

        await payments.AddEntryAsync(paymentId, new EntryInput(80, Now.AddDays(-7), PaymentMethod.Pix, "1ª parcela"), Ct);
        Assert.Equal(PaymentStatus.Parcial, (await payments.GetAsync(paymentId, Ct)).Status);

        await Assert.ThrowsAsync<BusinessException>(() => payments.AddEntryAsync(paymentId, new EntryInput(150, null, PaymentMethod.Pix, null), Ct));
        await Assert.ThrowsAsync<BusinessException>(() => payments.AddEntryAsync(paymentId, new EntryInput(10, Now.AddDays(1), PaymentMethod.Pix, null), Ct));

        await payments.AddEntryAsync(paymentId, new EntryInput(120, null, PaymentMethod.Cartao, null), Ct);
        var paid = await payments.GetAsync(paymentId, Ct);
        Assert.Equal((200m, PaymentStatus.Confirmado, Now.Date), (paid.PaidPrice, paid.Status, paid.PaidDate!.Value));

        await payments.DeleteEntryAsync(paid.Entries.Single(e => e.Amount == 120).Id, Ct);
        var reverted = await payments.GetAsync(paymentId, Ct);
        Assert.Equal((80m, PaymentStatus.Parcial), (reverted.PaidPrice, reverted.Status));
    }

    [Fact]
    public async Task Valor_pago_no_agendamento_vira_recebimento_com_forma()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(s, Now.AddDays(2)) with { PaidPrice = 50, PaymentMethod = PaymentMethod.Dinheiro }, Ct);

        var entry = await db.PaymentEntries.AsNoTracking().SingleAsync(Ct);
        Assert.Equal((50m, PaymentMethod.Dinheiro, Now.Date), (entry.Amount, entry.Method, entry.PaidAt));
    }

    [Fact]
    public async Task Valor_da_consulta_nao_fica_abaixo_do_recebido()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var appointments = Appointments(db);
        await appointments.CreateAsync(Input(s, Now.AddDays(2)) with { PaidPrice = 150 }, Ct);
        var id = await db.Appointments.Select(a => a.Id).SingleAsync(Ct);

        await Assert.ThrowsAsync<BusinessException>(() => appointments.UpdateAsync(id, Input(s, Now.AddDays(2)) with { Price = 100 }, ct: Ct));
    }

    [Fact]
    public async Task Pacote_cobre_sessoes_ate_acabar_e_avisa()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var packageId = await Payments(db).CreatePackageAsync(new PackageInput(s.ClientId, "Pacote 4", 4, 560, Now.AddDays(60)), Ct);

        var result = await Appointments(db).CreateAsync(
            Input(s, Now.AddDays(1)) with { UsePackage = true, Recurrence = RecurrenceFrequency.Semanal, Occurrences = 6 }, Ct);

        var all = await db.Appointments.AsNoTracking().Include(a => a.Payment).OrderBy(a => a.ScheduledAt).ToListAsync(Ct);
        Assert.All(all.Take(4), a => Assert.Equal((packageId, 0m, PaymentStatus.Confirmado), (a.PackageId!.Value, a.Payment!.Price, a.Payment.Status)));
        Assert.All(all.Skip(4), a => Assert.Equal((null as int?, 200m), (a.PackageId, a.Payment!.Price)));
        Assert.Contains(result.Warnings, w => w.Contains("4 de 6", StringComparison.Ordinal));

        var package = Assert.Single(await Payments(db).ListPackagesAsync(s.ClientId, Ct));
        Assert.Equal(0, package.RemainingSessions);
        await Assert.ThrowsAsync<BusinessException>(() => Payments(db).DeletePackageAsync(packageId, Ct));
    }

    [Fact]
    public async Task Agendar_com_pacote_sem_saldo_e_recusado_e_pagamento_do_pacote_tem_limite()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        await Assert.ThrowsAsync<BusinessException>(() => Appointments(db).CreateAsync(Input(s, Now.AddDays(1)) with { UsePackage = true }, Ct));

        var packageId = await Payments(db).CreatePackageAsync(new PackageInput(s.ClientId, "Pacote", 2, 300, null), Ct);
        await Payments(db).AddPackageEntryAsync(packageId, new EntryInput(300, null, PaymentMethod.Pix, null), Ct);
        await Assert.ThrowsAsync<BusinessException>(() =>
            Payments(db).AddPackageEntryAsync(packageId, new EntryInput(1, null, PaymentMethod.Pix, null), Ct));
    }

    [Fact]
    public async Task Tipo_de_servico_usado_e_desativado_em_vez_de_excluido()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var payments = Payments(db);
        var used = await payments.SaveServiceTypeAsync(new ServiceType { Name = "Sessão", DurationMinutes = 50, Price = 180 }, Ct);
        var unused = await payments.SaveServiceTypeAsync(new ServiceType { Name = "Avaliação", DurationMinutes = 60, Price = 300 }, Ct);
        await Appointments(db).CreateAsync(Input(s, Now.AddDays(1)) with { ServiceTypeId = used }, Ct);

        await payments.DeleteServiceTypeAsync(used, Ct);
        await payments.DeleteServiceTypeAsync(unused, Ct);

        var remaining = Assert.Single(await payments.ListServiceTypesAsync(ct: Ct));
        Assert.Equal((used, false), (remaining.Id, remaining.Active));
    }

    [Fact]
    public async Task Recibos_sao_gerados_em_pdf()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(s, Now.AddDays(-3)) with { PaidPrice = 200 }, Ct);
        var paymentId = await db.Payments.Select(p => p.Id).SingleAsync(Ct);
        var receipts = new ReceiptService(db, Scope(db), Options.Create(new ClinicLegalOptions { Cnpj = "12.345.678/0001-90" }), _clock);

        var single = await receipts.ForPaymentAsync(paymentId, Ct);
        var monthly = await receipts.MonthlyAsync(s.ClientId, Now.Year, Now.Month, Ct);

        Assert.All([single.Content, monthly.Content], pdf => Assert.Equal("%PDF"u8.ToArray(), pdf[..4]));
        Assert.EndsWith(".pdf", monthly.FileName, StringComparison.Ordinal);
        await Assert.ThrowsAsync<BusinessException>(() => receipts.MonthlyAsync(s.ClientId, 2020, 1, Ct));
    }

    [Fact]
    public async Task Relatorio_soma_faturamento_recebimentos_e_repasse()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var appointments = Appointments(db);
        await appointments.CreateAsync(Input(s, Now.AddDays(-3)) with { PaidPrice = 200, PaymentMethod = PaymentMethod.Pix }, Ct);
        await appointments.CreateAsync(Input(s, Now.AddDays(-2)) with { PaidPrice = 50, PaymentMethod = PaymentMethod.Cartao }, Ct);
        await appointments.CreateAsync(Input(s, Now.AddDays(1)), Ct);
        var first = await db.Appointments.OrderBy(a => a.ScheduledAt).Select(a => a.Id).FirstAsync(Ct);
        await appointments.SetStatusAsync(first, AppointmentStatus.Realizada, null, ct: Ct);

        var reports = new ReportService(db, Scope(db), _clock);
        var report = await reports.GetAsync(new DateTime(2026, 5, 1), new DateTime(2026, 6, 1), ct: Ct);

        Assert.Equal((600m, 250m, 150m), (report.Billed, report.Received, report.Outstanding));
        var professional = Assert.Single(report.Professionals);
        Assert.Equal((1, 250m, 150m), (professional.SessionsDone, professional.Received, professional.Commission)); // 60% de repasse
        Assert.Equal([PaymentMethod.Pix, PaymentMethod.Cartao], report.Methods.Select(m => m.Method));
        Assert.Equal(250m, report.Months.Single(m => m.Month == new DateTime(2026, 5, 1)).Received);

        var excel = reports.ToExcel(report);
        Assert.Equal("PK"u8.ToArray(), excel[..2]); // arquivo .xlsx (zip)
    }

    // ------------------------------------------------------------------

    private sealed record Seed(int ClientId, int ProfessionalId);

    private AppointmentService Appointments(MindCareDbContext db) => new(db, _clock);

    private PaymentService Payments(MindCareDbContext db) => new(db, _clock);

    private static AccessScope Scope(MindCareDbContext db) => new(db, new SystemActor { AccessLevel = AccessLevel.Administrador });

    private static AppointmentInput Input(Seed s, DateTime start) =>
        new(s.ClientId, AppointmentModality.Presencial, start, null, 200, 0) { ProfessionalId = s.ProfessionalId };

    private static async Task<Seed> SeedAsync(MindCareDbContext db)
    {
        var professional = new Professional
        {
            Name = "Letícia",
            Cpf = "15350946056",
            Gender = "Feminino",
            Speciality = "Psicoterapia",
            RegistrationNumber = "CRP 07/12345",
            CommissionPercent = 60,
            User = new User { Username = "15350946056", PasswordHash = "x" },
        };
        var client = new Client { Name = "Ana Moura", Cpf = "52998224725", Gender = "Feminino" };
        db.AddRange(professional, client);
        await db.SaveChangesAsync(Ct);
        return new Seed(client.Id, professional.Id);
    }

    public void Dispose() => _database.Dispose();
}
