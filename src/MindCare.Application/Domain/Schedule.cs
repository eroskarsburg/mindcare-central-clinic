namespace MindCare.Application.Domain;

/// <summary>Intervalo semanal em que o profissional atende (ex.: segunda, 08:00–12:00).</summary>
public sealed class ProfessionalAvailability
{
    public int Id { get; set; }
    public int ProfessionalId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public bool Contains(DateTime start, DateTime end) =>
        start.DayOfWeek == DayOfWeek
        && start.Date == end.AddTicks(-1).Date
        && TimeOnly.FromDateTime(start) >= StartTime
        && TimeOnly.FromDateTime(end) <= EndTime;
}

/// <summary>Período sem atendimento: férias de um profissional ou feriado da clínica inteira.</summary>
public sealed class ScheduleBlock
{
    public int Id { get; set; }

    /// <summary>Nulo quando o bloqueio vale para toda a clínica.</summary>
    public int? ProfessionalId { get; set; }
    public Professional? Professional { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Reason { get; set; } = string.Empty;

    public bool Overlaps(DateTime start, DateTime end) => StartsAt < end && EndsAt > start;
}
