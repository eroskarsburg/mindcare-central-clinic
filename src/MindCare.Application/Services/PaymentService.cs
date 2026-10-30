using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed record EntryInput(decimal Amount, DateTime? PaidAt, PaymentMethod Method, string? Note);

public sealed record PackageInput(int ClientId, string Name, int TotalSessions, decimal Price, DateTime? ExpiresAt);

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default);
    Task<Payment> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Registra um recebimento (parcela) de uma consulta.</summary>
    Task AddEntryAsync(int paymentId, EntryInput input, CancellationToken ct = default);
    Task DeleteEntryAsync(long entryId, CancellationToken ct = default);

    Task<IReadOnlyList<SessionPackage>> ListPackagesAsync(int clientId, CancellationToken ct = default);
    Task<int> CreatePackageAsync(PackageInput input, CancellationToken ct = default);
    Task AddPackageEntryAsync(int packageId, EntryInput input, CancellationToken ct = default);
    Task DeletePackageAsync(int packageId, CancellationToken ct = default);

    Task<IReadOnlyList<ServiceType>> ListServiceTypesAsync(bool onlyActive = false, CancellationToken ct = default);
    Task<int> SaveServiceTypeAsync(ServiceType input, CancellationToken ct = default);
    Task DeleteServiceTypeAsync(int id, CancellationToken ct = default);
}

internal sealed class PaymentService(MindCareDbContext db, TimeProvider clock, IAccessScope? scope = null) : IPaymentService
{
    private DateTime Today => clock.GetLocalNow().Date;

    public async Task<IReadOnlyList<Payment>> ListAsync(CancellationToken ct = default)
    {
        var payments = db.Payments.AsNoTracking()
            .Include(p => p.Entries)
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(p => p.Appointment!).ThenInclude(a => a.Professional)
            .Include(p => p.Appointment!).ThenInclude(a => a.Package)
            .AsQueryable();

        if (await RestrictedAsync(ct) is { } own)
        {
            payments = payments.Where(p => p.Appointment!.ProfessionalId == own);
        }

        return await payments.AsSplitQuery().OrderByDescending(p => p.Appointment!.ScheduledAt).ToListAsync(ct);
    }

    public async Task<Payment> GetAsync(int id, CancellationToken ct = default) =>
        await LoadPaymentAsync(id, tracking: false, ct);

    public async Task AddEntryAsync(int paymentId, EntryInput input, CancellationToken ct = default)
    {
        var payment = await LoadPaymentAsync(paymentId, tracking: true, ct);
        if (payment.Appointment?.PackageId is not null)
        {
            throw new BusinessException("Esta sessão é coberta por um pacote: registre o pagamento no pacote.");
        }

        ValidateAmount(input.Amount, payment.Outstanding);
        payment.Entries.Add(NewEntry(input));
        payment.SyncFromEntries(Today);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEntryAsync(long entryId, CancellationToken ct = default)
    {
        var entry = await db.PaymentEntries.FirstOrDefaultAsync(e => e.Id == entryId, ct)
            ?? throw new BusinessException("Recebimento não encontrado.");

        if (entry.PaymentId is { } paymentId)
        {
            var payment = await LoadPaymentAsync(paymentId, tracking: true, ct);
            payment.Entries.Remove(payment.Entries.First(e => e.Id == entryId));
            db.PaymentEntries.Remove(entry);
            payment.SyncFromEntries(Today);
        }
        else
        {
            await LoadPackageAsync(entry.PackageId!.Value, ct); // valida acesso
            db.PaymentEntries.Remove(entry);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SessionPackage>> ListPackagesAsync(int clientId, CancellationToken ct = default) =>
        await db.Packages.AsNoTracking()
            .Include(p => p.Entries)
            .Include(p => p.Appointments)
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.PurchasedAt)
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task<int> CreatePackageAsync(PackageInput input, CancellationToken ct = default)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == input.ClientId && c.AnonymizedAt == null, ct))
        {
            throw new BusinessException("Paciente não encontrado.");
        }

        if (input.TotalSessions is < 1 or > 100)
        {
            throw new BusinessException("O pacote deve ter de 1 a 100 sessões.");
        }

        if (input.Price < 0)
        {
            throw new BusinessException("Valor do pacote inválido.");
        }

        if (input.ExpiresAt is { } expires && expires.Date < Today)
        {
            throw new BusinessException("A validade do pacote não pode estar no passado.");
        }

        var package = new SessionPackage
        {
            ClientId = input.ClientId,
            Name = input.Name.Trim(),
            TotalSessions = input.TotalSessions,
            Price = input.Price,
            PurchasedAt = Today,
            ExpiresAt = input.ExpiresAt?.Date,
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync(ct);
        return package.Id;
    }

    public async Task AddPackageEntryAsync(int packageId, EntryInput input, CancellationToken ct = default)
    {
        var package = await LoadPackageAsync(packageId, ct);
        ValidateAmount(input.Amount, package.Price - package.PaidAmount);
        package.Entries.Add(NewEntry(input));
        await db.SaveChangesAsync(ct);
    }

    public async Task DeletePackageAsync(int packageId, CancellationToken ct = default)
    {
        var package = await LoadPackageAsync(packageId, ct);
        if (package.Appointments.Count > 0)
        {
            throw new BusinessException("Este pacote já tem sessões agendadas e não pode ser excluído.");
        }

        db.Packages.Remove(package);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceType>> ListServiceTypesAsync(bool onlyActive = false, CancellationToken ct = default) =>
        await db.ServiceTypes.AsNoTracking()
            .Where(s => !onlyActive || s.Active)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<int> SaveServiceTypeAsync(ServiceType input, CancellationToken ct = default)
    {
        var name = input.Name.Trim();
        if (await db.ServiceTypes.AnyAsync(s => s.Name == name && s.Id != input.Id, ct))
        {
            throw new BusinessException("Já existe um tipo de serviço com este nome.");
        }

        var service = input.Id == 0
            ? db.ServiceTypes.Add(new ServiceType()).Entity
            : await db.ServiceTypes.FindAsync([input.Id], ct) ?? throw new BusinessException("Tipo de serviço não encontrado.");

        service.Name = name;
        service.DurationMinutes = input.DurationMinutes;
        service.Price = input.Price;
        service.Active = input.Active;
        await db.SaveChangesAsync(ct);
        return service.Id;
    }

    public async Task DeleteServiceTypeAsync(int id, CancellationToken ct = default)
    {
        var service = await db.ServiceTypes.FindAsync([id], ct) ?? throw new BusinessException("Tipo de serviço não encontrado.");
        if (await db.Appointments.AnyAsync(a => a.ServiceTypeId == id, ct))
        {
            // Mantém o histórico: tipos já usados são apenas desativados.
            service.Active = false;
        }
        else
        {
            db.ServiceTypes.Remove(service);
        }

        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private Task<int?> RestrictedAsync(CancellationToken ct) =>
        scope?.RestrictedProfessionalIdAsync(ct) ?? Task.FromResult<int?>(null);

    private async Task<Payment> LoadPaymentAsync(int id, bool tracking, CancellationToken ct)
    {
        var query = db.Payments
            .Include(p => p.Entries)
            .Include(p => p.Appointment!).ThenInclude(a => a.Client)
            .Include(p => p.Appointment!).ThenInclude(a => a.Professional)
            .AsQueryable();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var payment = await query.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new BusinessException("Pagamento não encontrado.");

        if (await RestrictedAsync(ct) is { } own && payment.Appointment?.ProfessionalId != own)
        {
            throw new BusinessException("Você só pode acessar pagamentos dos seus próprios atendimentos.");
        }

        return payment;
    }

    private async Task<SessionPackage> LoadPackageAsync(int id, CancellationToken ct) =>
        await db.Packages.Include(p => p.Entries).Include(p => p.Appointments).AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new BusinessException("Pacote não encontrado.");

    private PaymentEntry NewEntry(EntryInput input)
    {
        var paidAt = (input.PaidAt ?? Today).Date;
        if (paidAt > Today)
        {
            throw new BusinessException("A data do recebimento não pode estar no futuro.");
        }

        return new PaymentEntry
        {
            Amount = input.Amount,
            PaidAt = paidAt,
            Method = input.Method,
            Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
            CreatedAt = clock.GetLocalNow().DateTime,
        };
    }

    private static void ValidateAmount(decimal amount, decimal outstanding)
    {
        if (amount <= 0)
        {
            throw new BusinessException("Informe um valor maior que zero.");
        }

        if (amount > outstanding)
        {
            throw new BusinessException($"O valor excede o saldo em aberto ({outstanding:C}).");
        }
    }
}
