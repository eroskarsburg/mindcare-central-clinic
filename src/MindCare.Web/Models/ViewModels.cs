using MindCare.Application.Domain;

namespace MindCare.Web.Models;

public sealed record AppointmentsPageViewModel(IReadOnlyList<Appointment> Appointments, IReadOnlyList<Client> Clients);

public sealed record ProfileViewModel(User User, Professional? Professional);

public sealed record ErrorViewModel(int StatusCode, string Title, string Message, string? RequestId);
