using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

/// <summary>Usuário autenticado e o nome a exibir na interface.</summary>
public sealed record AuthenticatedUser(int Id, string Username, string DisplayName, AccessLevel AccessLevel);

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);
}

internal sealed class AuthService(MindCareDbContext db, IPasswordService passwords, TimeProvider clock) : IAuthService
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Professional)
            .FirstOrDefaultAsync(u => u.Username == username.Trim(), ct);

        if (user is null || !passwords.Verify(user, password, out var needsRehash))
        {
            return null;
        }

        if (needsRehash)
        {
            user.PasswordHash = passwords.Hash(password);
        }

        user.LastActivity = clock.GetLocalNow().DateTime;
        await db.SaveChangesAsync(ct);

        var displayName = string.IsNullOrWhiteSpace(user.Professional?.Name) ? user.Username : user.Professional.Name;
        return new AuthenticatedUser(user.Id, user.Username, displayName, user.AccessLevel);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct) ?? throw new BusinessException("Usuário não encontrado.");

        if (!passwords.Verify(user, currentPassword, out _))
        {
            throw new BusinessException("A senha atual está incorreta.");
        }

        user.PasswordHash = passwords.Hash(newPassword);
        await db.SaveChangesAsync(ct);
    }
}
