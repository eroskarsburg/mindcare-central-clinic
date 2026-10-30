using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;

namespace MindCare.Application.Services;

public interface IClientService
{
    Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default);
    Task<int> CreateAsync(Client input, CancellationToken ct = default);
    Task UpdateAsync(Client input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

internal sealed class ClientService(MindCareDbContext db) : IClientService
{
    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default) =>
        await db.Clients.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<int> CreateAsync(Client input, CancellationToken ct = default)
    {
        var client = new Client();
        await ApplyAsync(client, input, ct);
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client.Id;
    }

    public async Task UpdateAsync(Client input, CancellationToken ct = default)
    {
        var client = await db.Clients.FindAsync([input.Id], ct)
            ?? throw new BusinessException("Paciente não encontrado.");
        await ApplyAsync(client, input, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (await db.Appointments.AnyAsync(a => a.ClientId == id, ct))
        {
            throw new BusinessException("Este paciente possui agendamentos. Remova-os antes de excluir o cadastro.");
        }

        await db.Clients.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
    }

    private async Task ApplyAsync(Client target, Client input, CancellationToken ct)
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
    }
}
