namespace MindCare.Application.Domain;

/// <summary>Conta de acesso ao painel.</summary>
public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Profissional;
    public DateTime? LastActivity { get; set; }

    /// <summary>Exige a troca de senha no próximo acesso (senha definida por outra pessoa).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Tentativas de login malsucedidas consecutivas.</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Até quando o login está bloqueado após excesso de tentativas.</summary>
    public DateTime? LockoutUntil { get; set; }

    public Professional? Professional { get; set; }
}
