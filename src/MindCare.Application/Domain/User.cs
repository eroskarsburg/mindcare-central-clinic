namespace MindCare.Application.Domain;

/// <summary>Conta de acesso ao painel.</summary>
public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Profissional;
    public DateTime? LastActivity { get; set; }

    public Professional? Professional { get; set; }
}
