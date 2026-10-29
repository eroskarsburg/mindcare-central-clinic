namespace MindCare.Application.Domain;

/// <summary>Registro de auditoria: quem fez o quê, quando e de onde (LGPD).</summary>
public sealed class AuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    /// <summary>Resumo da operação. Nunca contém valores sensíveis, apenas nomes de campos.</summary>
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public static class AuditActions
{
    public const string Created = "Criação";
    public const string Updated = "Alteração";
    public const string Deleted = "Exclusão";
    public const string Viewed = "Visualização";
    public const string Exported = "Exportação";
    public const string Anonymized = "Anonimização";
    public const string LoginSucceeded = "Login";
    public const string LoginFailed = "Login recusado";
    public const string LockedOut = "Bloqueio de conta";
    public const string PasswordChanged = "Troca de senha";
}
