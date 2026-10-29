using MindCare.Application.Domain;

namespace MindCare.Web.Models;

public sealed record AppointmentsPageViewModel(
    IReadOnlyList<Appointment> Appointments,
    IReadOnlyList<Client> Clients,
    IReadOnlyList<Professional> Professionals,
    int? RestrictedProfessionalId);

public sealed record SchedulePageViewModel(
    MindCare.Application.Services.ProfessionalSchedule Schedule,
    IReadOnlyList<Professional> Professionals,
    bool CanChooseProfessional);

public sealed record ClientsPageViewModel(IReadOnlyList<Client> Clients, IReadOnlyList<Professional> Professionals);

public sealed record ClientDetailsViewModel(
    MindCare.Application.Services.ClientDetails Details,
    MindCare.Application.Records.NotesResult Notes,
    IReadOnlyList<ClientDocument> Documents,
    IReadOnlyList<Professional> Professionals,
    int MaxUploadMegabytes);

public sealed record ProfileViewModel(User User, Professional? Professional);

public sealed record ErrorViewModel(int StatusCode, string Title, string Message, string? RequestId);
