namespace MindCare.Application.Domain;

/// <summary>Consulta agendada para um paciente, com o respectivo pagamento.</summary>
public sealed class Appointment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public AppointmentModality Modality { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Observation { get; set; }
    public Payment? Payment { get; set; }
}
