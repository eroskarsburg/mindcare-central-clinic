using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed class SecurityOptions
{
    public const string Section = "Security";

    /// <summary>Tentativas de login malsucedidas antes do bloqueio temporário.</summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>Duração do bloqueio, em minutos.</summary>
    public int LockoutMinutes { get; set; } = 15;
}

/// <summary>Usuário autenticado e o nome a exibir na interface.</summary>
public sealed record AuthenticatedUser(int Id, string Username, string DisplayName, AccessLevel AccessLevel, bool MustChangePassword);

public enum LoginFailure
{
    None,
    InvalidCredentials,
    LockedOut,
}

public sealed record LoginResult(AuthenticatedUser? User, LoginFailure Failure, DateTime? LockedUntil = null)
{
    public bool Succeeded => User is not null;
}

public interface IAuthService
{
    Task<LoginResult> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task<AuthenticatedUser?> GetSessionUserAsync(int userId, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);
}

internal sealed class AuthService(
    MindCareDbContext db,
    IPasswordService passwords,
    IOptions<SecurityOptions> options,
    ICurrentActor actor,
    TimeProvider clock) : IAuthService
{
    public async Task<LoginResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var now = clock.GetLocalNow().DateTime;
        var user = await db.Users.Include(u => u.Professional)
            .FirstOrDefaultAsync(u => u.Username == username.Trim(), ct);

        if (user is null)
        {
            Log(null, username, AuditActions.LoginFailed, "Usuário inexistente", now);
            await db.SaveChangesAsync(ct);
            return new LoginResult(null, LoginFailure.InvalidCredentials);
        }

        if (user.LockoutUntil > now)
        {
            Log(user.Id, user.Username, AuditActions.LoginFailed, "Conta bloqueada", now);
            await db.SaveChangesAsync(ct);
            return new LoginResult(null, LoginFailure.LockedOut, user.LockoutUntil);
        }

        if (!passwords.Verify(user, password, out var needsRehash))
        {
            user.FailedLoginCount++;
            Log(user.Id, user.Username, AuditActions.LoginFailed, $"Senha incorreta ({user.FailedLoginCount}ª tentativa)", now);

            if (user.FailedLoginCount >= options.Value.MaxFailedLoginAttempts)
            {
                user.FailedLoginCount = 0;
                user.LockoutUntil = now.AddMinutes(options.Value.LockoutMinutes);
                Log(user.Id, user.Username, AuditActions.LockedOut, $"Bloqueado por {options.Value.LockoutMinutes} minutos", now);
                await db.SaveChangesAsync(ct);
                return new LoginResult(null, LoginFailure.LockedOut, user.LockoutUntil);
            }

            await db.SaveChangesAsync(ct);
            return new LoginResult(null, LoginFailure.InvalidCredentials);
        }

        if (needsRehash)
        {
            user.PasswordHash = passwords.Hash(password);
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.LastActivity = now;
        Log(user.Id, user.Username, AuditActions.LoginSucceeded, null, now);
        await db.SaveChangesAsync(ct);

        return new LoginResult(ToSession(user), LoginFailure.None);
    }

    public async Task<AuthenticatedUser?> GetSessionUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Professional).FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : ToSession(user);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct) ?? throw new BusinessException("Usuário não encontrado.");

        if (!passwords.Verify(user, currentPassword, out _))
        {
            throw new BusinessException("A senha atual está incorreta.");
        }

        if (currentPassword == newPassword)
        {
            throw new BusinessException("A nova senha deve ser diferente da atual.");
        }

        user.PasswordHash = passwords.Hash(newPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(ct);
    }

    private static AuthenticatedUser ToSession(User user)
    {
        var displayName = string.IsNullOrWhiteSpace(user.Professional?.Name) ? user.Username : user.Professional.Name;
        return new AuthenticatedUser(user.Id, user.Username, displayName, user.AccessLevel, user.MustChangePassword);
    }

    // No login ainda não há usuário autenticado: o log usa o usuário que tentou entrar.
    private void Log(int? userId, string username, string action, string? details, DateTime now) =>
        db.AuditLogs.Add(new AuditLog
        {
            Timestamp = now,
            UserId = userId,
            Username = username.Length > 50 ? username[..50] : username,
            IpAddress = actor.IpAddress,
            Action = action,
            Entity = Audit.EntityName<User>(),
            EntityId = userId?.ToString(),
            Details = details,
        });
}
