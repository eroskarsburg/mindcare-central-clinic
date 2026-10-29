namespace MindCare.Application.Domain;

/// <summary>Indicadores exibidos na página inicial.</summary>
public sealed record DashboardSummary(
    int AppointmentsToday,
    int AppointmentsNext7Days,
    int TotalClients,
    decimal OutstandingAmount,
    decimal ReceivedThisMonth);
