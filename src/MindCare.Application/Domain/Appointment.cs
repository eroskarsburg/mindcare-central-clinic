namespace MindCare.Application.Domain;

/// <summary>Consulta agendada para um paciente, com o respectivo pagamento.</summary>
public sealed class Appointment
{
    public const int DefaultDurationMinutes = 50;
    public const int MaxDurationMinutes = 240;

    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    /// <summary>Profissional responsável. Nulo apenas em consultas legadas, anteriores a este campo.</summary>
    public int? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }

    public AppointmentModality Modality { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = DefaultDurationMinutes;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Agendada;
    public string? CancellationReason { get; set; }

    /// <summary>Identifica as consultas criadas juntas como uma série recorrente.</summary>
    public Guid? SeriesId { get; set; }

    /// <summary>Sala de teleconsulta (gerada automaticamente para consultas online).</summary>
    public string? MeetingUrl { get; set; }

    public string? Observation { get; set; }
    public Payment? Payment { get; set; }

    public DateTime EndsAt => ScheduledAt.AddMinutes(DurationMinutes);

    /// <summary>Consultas canceladas liberam o horário e não contam como conflito.</summary>
    public bool OccupiesSlot => Status != AppointmentStatus.Cancelada;

    public bool Overlaps(DateTime start, DateTime end) => ScheduledAt < end && EndsAt > start;
}
