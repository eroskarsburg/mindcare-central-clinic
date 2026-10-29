namespace MindCare.Application.Domain;

/// <summary>Paciente da clínica.</summary>
public sealed class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;

    /// <summary>Idade informada manualmente (cadastros antigos). Prefira <see cref="BirthDate"/>.</summary>
    public int? Age { get; set; }
    public DateTime? BirthDate { get; set; }

    public ClientStatus Status { get; set; } = ClientStatus.Ativo;

    /// <summary>Profissional de referência: lê o prontuário e os documentos clínicos.</summary>
    public int? ResponsibleProfessionalId { get; set; }
    public Professional? ResponsibleProfessional { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Address { get; set; }
    public string? HealthInsurance { get; set; }

    /// <summary>Observações administrativas (não clínicas), visíveis à equipe.</summary>
    public string? Notes { get; set; }

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

    /// <summary>Idade calculada pela data de nascimento, ou a informada manualmente.</summary>
    public int? AgeOn(DateTime today)
    {
        if (BirthDate is not { } birth)
        {
            return Age;
        }

        var age = today.Year - birth.Year;
        return birth.Date > today.AddYears(-age) ? age - 1 : age;
    }

    public ICollection<Appointment> Appointments { get; } = [];
}
