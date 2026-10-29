using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Services;

/// <summary>Dados do titular para atender a uma solicitação de portabilidade/acesso (LGPD, art. 18).</summary>
public sealed record ClientDataExport(
    DateTime GeneratedAt,
    object Patient,
    IReadOnlyList<object> Appointments);

public interface IClientService
{
    Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default);
    Task<int> CreateAsync(Client input, bool consentGiven, CancellationToken ct = default);
    Task UpdateAsync(Client input, bool consentGiven, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<ClientDataExport> ExportAsync(int id, CancellationToken ct = default);
    Task AnonymizeAsync(int id, CancellationToken ct = default);
}

internal sealed class ClientService(MindCareDbContext db, IAuditService audit, TimeProvider clock) : IClientService
{
    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default) =>
        await db.Clients.AsNoTracking().OrderBy(c => c.AnonymizedAt != null).ThenBy(c => c.Name).ToListAsync(ct);

    public async Task<int> CreateAsync(Client input, bool consentGiven, CancellationToken ct = default)
    {
        var client = new Client();
        await ApplyAsync(client, input, consentGiven, ct);
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client.Id;
    }

    public async Task UpdateAsync(Client input, bool consentGiven, CancellationToken ct = default)
    {
        var client = await FindEditableAsync(input.Id, ct);
        await ApplyAsync(client, input, consentGiven, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (await db.Appointments.AnyAsync(a => a.ClientId == id, ct))
        {
            throw new BusinessException(
                "Este paciente possui agendamentos. Remova-os ou use a anonimização para atender a pedidos de exclusão de dados.");
        }

        var client = await db.Clients.FindAsync([id], ct) ?? throw new BusinessException("Paciente não encontrado.");
        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ClientDataExport> ExportAsync(int id, CancellationToken ct = default)
    {
        var client = await db.Clients.AsNoTracking()
            .Include(c => c.Appointments).ThenInclude(a => a.Payment)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new BusinessException("Paciente não encontrado.");

        await audit.LogAsync(AuditActions.Exported, Audit.EntityName<Client>(), id.ToString(), ct: ct);

        return new ClientDataExport(
            clock.GetLocalNow().DateTime,
            new
            {
                client.Id,
                client.Name,
                Cpf = Cpf.Format(client.Cpf),
                client.Gender,
                client.Age,
                Phone = Phone.Format(client.Phone),
                client.Email,
                client.NotifyByWhatsApp,
                client.NotifyByEmail,
                client.ConsentAt,
                client.AnonymizedAt,
            },
            [.. client.Appointments.OrderBy(a => a.ScheduledAt).Select(a => (object)new
            {
                a.ScheduledAt,
                Modality = a.Modality.ToString(),
                a.Observation,
                Price = a.Payment?.Price,
                PaidPrice = a.Payment?.PaidPrice,
                PaidDate = a.Payment?.PaidDate,
                Status = a.Payment?.Status.ToString(),
            })]);
    }

    /// <summary>
    /// Remove os dados pessoais do paciente, preservando o histórico financeiro e de agenda
    /// de forma não identificável (direito de eliminação, LGPD art. 18, VI).
    /// </summary>
    public async Task AnonymizeAsync(int id, CancellationToken ct = default)
    {
        var client = await FindEditableAsync(id, ct);

        client.Name = $"Paciente anonimizado #{client.Id}";
        client.Cpf = $"X{client.Id:D10}"; // não numérico: nunca colide com um CPF real
        client.Gender = "Outro";
        client.Age = null;
        client.Phone = null;
        client.Email = null;
        client.NotifyByWhatsApp = false;
        client.NotifyByEmail = false;
        client.AnonymizedAt = clock.GetLocalNow().DateTime;

        await db.Appointments.Where(a => a.ClientId == id && a.Observation != null)
            .ForEachAsync(a => a.Observation = null, ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(AuditActions.Anonymized, Audit.EntityName<Client>(), id.ToString(), ct: ct);
    }

    private async Task<Client> FindEditableAsync(int id, CancellationToken ct)
    {
        var client = await db.Clients.FindAsync([id], ct) ?? throw new BusinessException("Paciente não encontrado.");
        return client.IsAnonymized
            ? throw new BusinessException("Este cadastro foi anonimizado e não pode mais ser alterado.")
            : client;
    }

    private async Task ApplyAsync(Client target, Client input, bool consentGiven, CancellationToken ct)
    {
        var cpf = Cpf.Normalize(input.Cpf);
        if (await db.Clients.AnyAsync(c => c.Cpf == cpf && c.Id != target.Id, ct))
        {
            throw new BusinessException("Já existe um paciente cadastrado com este CPF.");
        }

        target.Name = input.Name.Trim();
        target.Cpf = cpf;
        target.Gender = input.Gender;
        target.Age = input.Age;
        target.Phone = Phone.Normalize(input.Phone);
        target.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim().ToLowerInvariant();
        target.NotifyByWhatsApp = input.NotifyByWhatsApp;
        target.NotifyByEmail = input.NotifyByEmail;

        if (consentGiven)
        {
            target.ConsentAt ??= clock.GetLocalNow().DateTime;
        }
        else
        {
            target.ConsentAt = null;
        }
    }
}
