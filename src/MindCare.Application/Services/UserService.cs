using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task<User?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(string username, string password, AccessLevel accessLevel, CancellationToken ct = default);
    Task UpdateAsync(int id, string username, string? newPassword, AccessLevel accessLevel, int currentUserId, CancellationToken ct = default);
    Task DeleteAsync(int id, int currentUserId, CancellationToken ct = default);
}

internal sealed class UserService(MindCareDbContext db, IPasswordService passwords) : IUserService
{
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking().Include(u => u.Professional).OrderBy(u => u.Username).ToListAsync(ct);

    public Task<User?> GetAsync(int id, CancellationToken ct = default) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<int> CreateAsync(string username, string password, AccessLevel accessLevel, CancellationToken ct = default)
    {
        username = username.Trim();
        await EnsureUniqueAsync(username, 0, ct);

        var user = new User { Username = username, PasswordHash = passwords.Hash(password), AccessLevel = accessLevel };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task UpdateAsync(int id, string username, string? newPassword, AccessLevel accessLevel, int currentUserId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new BusinessException("Usuário não encontrado.");
        username = username.Trim();
        await EnsureUniqueAsync(username, id, ct);

        if (user.AccessLevel == AccessLevel.Administrador && accessLevel != AccessLevel.Administrador)
        {
            if (id == currentUserId)
            {
                throw new BusinessException("Você não pode remover o seu próprio acesso de administrador.");
            }

            await EnsureAnotherAdminAsync(ct);
        }

        user.Username = username;
        user.AccessLevel = accessLevel;
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.PasswordHash = passwords.Hash(newPassword);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int currentUserId, CancellationToken ct = default)
    {
        if (id == currentUserId)
        {
            throw new BusinessException("Você não pode excluir o seu próprio usuário.");
        }

        var user = await db.Users.FindAsync([id], ct) ?? throw new BusinessException("Usuário não encontrado.");
        if (user.AccessLevel == AccessLevel.Administrador)
        {
            await EnsureAnotherAdminAsync(ct);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniqueAsync(string username, int ignoreId, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != ignoreId, ct))
        {
            throw new BusinessException("Este nome de usuário já está em uso.");
        }
    }

    private async Task EnsureAnotherAdminAsync(CancellationToken ct)
    {
        if (await db.Users.CountAsync(u => u.AccessLevel == AccessLevel.Administrador, ct) <= 1)
        {
            throw new BusinessException("É necessário manter pelo menos um administrador.");
        }
    }
}
