namespace MindCare.Application.Domain;

/// <summary>Paciente da clínica.</summary>
public sealed class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int? Age { get; set; }

    public ICollection<Appointment> Appointments { get; } = [];
}
