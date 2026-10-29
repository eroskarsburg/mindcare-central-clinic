using MindCare.Application.Domain;

namespace MindCare.Application.Security;

/// <summary>
/// Quem está executando a operação atual. Implementado pela camada web a partir do usuário autenticado;
/// em processos em segundo plano (seed, lembretes) não há usuário.
/// </summary>
public interface ICurrentActor
{
    int? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    AccessLevel? AccessLevel { get; }

    bool IsAdmin => AccessLevel == Domain.AccessLevel.Administrador;
}

/// <summary>Ator usado quando não há requisição HTTP (seed, jobs, testes).</summary>
public sealed class SystemActor : ICurrentActor
{
    public int? UserId { get; init; }
    public string? Username { get; init; } = "sistema";
    public string? IpAddress { get; init; }
    public AccessLevel? AccessLevel { get; init; }
}
