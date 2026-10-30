namespace MindCare.Application.Domain;

/// <summary>
/// Registro de evolução clínica (prontuário). O conteúdo é gravado criptografado e só pode ser lido
/// pelo profissional que o escreveu ou pelo profissional de referência do paciente.
/// </summary>
public sealed class ClinicalNote
{
    public long Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    /// <summary>Profissional autor.</summary>
    public int ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    /// <summary>Sessão a que a evolução se refere (opcional).</summary>
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    /// <summary>Texto protegido com ASP.NET Data Protection. Nunca contém texto puro.</summary>
    public string EncryptedContent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Arquivo anexado ao cadastro do paciente (laudos, termos, encaminhamentos).</summary>
public sealed class ClientDocument
{
    public long Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public DocumentCategory Category { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Nome do arquivo no armazenamento (aleatório; não revela o original).</summary>
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string? Description { get; set; }
    public int UploadedByUserId { get; set; }
    public int? UploadedByProfessionalId { get; set; }
    public DateTime UploadedAt { get; set; }
}
