using Microsoft.EntityFrameworkCore;
using MindCare.Application.Persistence;

namespace MindCare.Application.Security;

/// <summary>
/// Define o que o usuário atual pode ver na agenda e no financeiro:
/// usuários vinculados a um profissional enxergam apenas os próprios atendimentos;
/// administradores e usuários sem vínculo (ex.: recepção) enxergam a clínica inteira.
/// </summary>
public interface IAccessScope
{
    /// <summary>Id do profissional ao qual o acesso está restrito, ou nulo quando o acesso é total.</summary>
    Task<int?> RestrictedProfessionalIdAsync(CancellationToken ct = default);

    /// <summary>Profissional vinculado ao usuário atual (mesmo que seja administrador), ou nulo.</summary>
    Task<int?> OwnProfessionalIdAsync(CancellationToken ct = default);
}

internal sealed class AccessScope(MindCareDbContext db, ICurrentActor actor) : IAccessScope
{
    private bool _resolved;
    private int? _professionalId;

    public async Task<int?> RestrictedProfessionalIdAsync(CancellationToken ct = default) =>
        actor.IsAdmin ? null : await OwnProfessionalIdAsync(ct);

    public async Task<int?> OwnProfessionalIdAsync(CancellationToken ct = default)
    {
        if (_resolved)
        {
            return _professionalId;
        }

        _resolved = true;
        if (actor.UserId is not { } userId)
        {
            return null;
        }

        _professionalId = await db.Professionals.Where(p => p.UserId == userId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);
        return _professionalId;
    }
}
