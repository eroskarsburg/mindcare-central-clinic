namespace MindCare.Application.Domain;

/// <summary>Profissional de saúde vinculado a um usuário do sistema.</summary>
public sealed class Professional
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Speciality { get; set; } = string.Empty;

    /// <summary>Registro no conselho (ex.: CRP 07/12345), impresso nos recibos.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Percentual do valor recebido repassado ao profissional.</summary>
    public decimal CommissionPercent { get; set; }

    public ICollection<ProfessionalAvailability> Availability { get; } = [];
}
