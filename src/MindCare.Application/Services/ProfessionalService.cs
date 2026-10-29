using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

/// <summary>Credenciais geradas para o novo profissional — exibidas uma única vez ao administrador.</summary>
public sealed record ProfessionalCreated(int Id, string Username, string InitialPassword);

public interface IProfessionalService
{
    Task<IReadOnlyList<Professional>> ListAsync(CancellationToken ct = default);
    Task<Professional?> GetByUserAsync(int userId, CancellationToken ct = default);
    Task<ProfessionalCreated> CreateAsync(Professional input, CancellationToken ct = default);
    Task UpdateAsync(Professional input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

internal sealed class ProfessionalService(MindCareDbContext db, IPasswordService passwords) : IProfessionalService
{
    public async Task<IReadOnlyList<Professional>> ListAsync(CancellationToken ct = default) =>
        await db.Professionals.AsNoTracking().Include(p => p.User).OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Professional?> GetByUserAsync(int userId, CancellationToken ct = default) =>
        db.Professionals.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);

    /// <summary>
    /// Cadastra o profissional e cria seu usuário de acesso.
    /// Login: CPF · Senha inicial: aleatória, exibida uma única vez e trocada obrigatoriamente no primeiro acesso.
    /// </summary>
    public async Task<ProfessionalCreated> CreateAsync(Professional input, CancellationToken ct = default)
    {
        var professional = new Professional();
        await ApplyAsync(professional, input, ct);

        if (await db.Users.AnyAsync(u => u.Username == professional.Cpf, ct))
        {
            throw new BusinessException("Já existe um usuário com este CPF como login.");
        }

        var initialPassword = PasswordGenerator.Generate();
        professional.User = new User
        {
            Username = professional.Cpf,
            PasswordHash = passwords.Hash(initialPassword),
            AccessLevel = AccessLevel.Profissional,
            MustChangePassword = true,
        };

        db.Professionals.Add(professional);
        await db.SaveChangesAsync(ct);
        return new ProfessionalCreated(professional.Id, professional.Cpf, initialPassword);
    }

    public async Task UpdateAsync(Professional input, CancellationToken ct = default)
    {
        var professional = await db.Professionals.FindAsync([input.Id], ct)
            ?? throw new BusinessException("Profissional não encontrado.");
        await ApplyAsync(professional, input, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var professional = await db.Professionals.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new BusinessException("Profissional não encontrado.");

        db.Professionals.Remove(professional);
        if (professional.User is not null)
        {
            db.Users.Remove(professional.User);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(Professional target, Professional input, CancellationToken ct)
    {
        var cpf = Cpf.Normalize(input.Cpf);
        if (await db.Professionals.AnyAsync(p => p.Cpf == cpf && p.Id != target.Id, ct))
        {
            throw new BusinessException("Já existe um profissional cadastrado com este CPF.");
        }

        target.Name = input.Name.Trim();
        target.Cpf = cpf;
        target.Gender = input.Gender;
        target.Speciality = input.Speciality.Trim();
    }
}
