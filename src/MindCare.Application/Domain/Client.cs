namespace MindCare.Application.Domain;

/// <summary>Paciente da clínica.</summary>
public sealed class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int? Age { get; set; }

    /// <summary>Celular com DDI, somente dígitos (ex.: 5551999998888).</summary>
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Aceita receber confirmações e lembretes por WhatsApp.</summary>
    public bool NotifyByWhatsApp { get; set; } = true;

    /// <summary>Aceita receber confirmações e lembretes por e-mail.</summary>
    public bool NotifyByEmail { get; set; } = true;

    /// <summary>Data em que o termo de consentimento (LGPD) foi registrado.</summary>
    public DateTime? ConsentAt { get; set; }

    /// <summary>Data em que os dados pessoais foram anonimizados a pedido do titular.</summary>
    public DateTime? AnonymizedAt { get; set; }

    public bool IsAnonymized => AnonymizedAt is not null;

    public ICollection<Appointment> Appointments { get; } = [];
}
