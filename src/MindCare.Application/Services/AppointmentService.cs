using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Notifications;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed class TeleconsultOptions
{
    public const string Section = "Teleconsult";

    /// <summary>Servidor de videochamada usado para gerar as salas (Jitsi público por padrão).</summary>
    public string BaseUrl { get; set; } = "https://meet.jit.si";
}

/// <summary>Dados para criar ou alterar um agendamento e seu pagamento.</summary>
public sealed record AppointmentInput(
    int ClientId,
    AppointmentModality Modality,
    DateTime ScheduledAt,
    string? Observation,
    decimal Price,
    decimal PaidPrice)
{
    public int? ProfessionalId { get; init; }
    public int DurationMinutes { get; init; } = Appointment.DefaultDurationMinutes;
    public RecurrenceFrequency Recurrence { get; init; } = RecurrenceFrequency.Nenhuma;

    /// <summary>Total de sessões da série (incluindo a primeira).</summary>
    public int Occurrences { get; init; } = 1;

    public AppointmentStatus? Status { get; init; }
    public string? CancellationReason { get; init; }

    public int? ServiceTypeId { get; init; }

    /// <summary>Cobre as sessões com o pacote ativo do paciente (sem cobrança avulsa).</summary>
    public bool UsePackage { get; init; }

    /// <summary>Forma do valor já pago informado no agendamento.</summary>
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.Pix;
}

public sealed record AppointmentQuery(DateTime? From = null, DateTime? To = null, int? ProfessionalId = null, int? ClientId = null);

/// <summary>Resultado de uma gravação: quantas consultas foram afetadas e avisos não bloqueantes.</summary>
public sealed record ScheduleResult(int Affected, IReadOnlyList<string> Warnings)
{
    public string Describe(string singular, string plural) =>
        (Affected == 1 ? singular : $"{Affected} {plural}") + (Warnings.Count > 0 ? " " + string.Join(" ", Warnings) : string.Empty);
}

public interface IAppointmentService
{
    Task<IReadOnlyList<Appointment>> ListAsync(AppointmentQuery? query = null, CancellationToken ct = default);
    Task<ScheduleResult> CreateAsync(AppointmentInput input, CancellationToken ct = default);
    Task<ScheduleResult> UpdateAsync(int id, AppointmentInput input, SeriesScope scope = SeriesScope.ApenasEsta, CancellationToken ct = default);
    Task<ScheduleResult> RescheduleAsync(int id, DateTime start, int? durationMinutes, CancellationToken ct = default);
    Task<ScheduleResult> SetStatusAsync(int id, AppointmentStatus status, string? reason, SeriesScope scope = SeriesScope.ApenasEsta, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

internal sealed class AppointmentService(
    MindCareDbContext db,
    TimeProvider clock,
    IAccessScope? scope = null,
    IOptions<TeleconsultOptions>? teleconsult = null,
    INotificationOutbox? notifications = null) : IAppointmentService
{
    private const int MaxOccurrences = 52;

    private DateTime Now => clock.GetLocalNow().DateTime;

    public async Task<IReadOnlyList<Appointment>> ListAsync(AppointmentQuery? query = null, CancellationToken ct = default)
    {
        query ??= new AppointmentQuery();
        var appointments = db.Appointments.AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Professional)
            .Include(a => a.Payment)
            .AsQueryable();

        var professionalId = await RestrictedAsync(ct) ?? query.ProfessionalId;
        if (professionalId is not null)
        {
            appointments = appointments.Where(a => a.ProfessionalId == professionalId);
        }

        if (query.ClientId is { } clientId)
        {
            appointments = appointments.Where(a => a.ClientId == clientId);
        }

        if (query.From is { } from)
        {
            appointments = appointments.Where(a => a.ScheduledAt >= from.AddMinutes(-Appointment.MaxDurationMinutes));
        }

        if (query.To is { } to)
        {
            appointments = appointments.Where(a => a.ScheduledAt < to);
        }

        return await appointments.OrderByDescending(a => a.ScheduledAt).ToListAsync(ct);
    }

    public async Task<ScheduleResult> CreateAsync(AppointmentInput input, CancellationToken ct = default)
    {
        var professionalId = await ResolveProfessionalAsync(input.ProfessionalId, ct);
        await ValidateInputAsync(input, ct);

        var occurrences = input.Recurrence == RecurrenceFrequency.Nenhuma ? 1 : input.Occurrences;
        if (occurrences is < 1 or > MaxOccurrences)
        {
            throw new BusinessException($"Informe de 1 a {MaxOccurrences} sessões para a série.");
        }

        var step = input.Recurrence == RecurrenceFrequency.Quinzenal ? 14 : 7;
        var seriesId = occurrences > 1 ? Guid.NewGuid() : (Guid?)null;
        var meetingUrl = input.Modality == AppointmentModality.Teleconsulta ? NewMeetingUrl() : null;

        var package = input.UsePackage ? await FindPackageAsync(input.ClientId, input.ScheduledAt, ct) : null;
        var packageSlots = package?.RemainingSessions ?? 0;

        var created = new List<Appointment>();
        var skipped = new List<DateTime>();
        for (var i = 0; i < occurrences; i++)
        {
            var start = input.ScheduledAt.AddDays(step * i);
            var end = start.AddMinutes(input.DurationMinutes);

            // Em séries, datas bloqueadas (férias, feriados) são puladas; numa consulta avulsa, bloqueiam.
            if (await FindBlockAsync(professionalId, start, end, ct) is { } block)
            {
                if (occurrences == 1)
                {
                    throw new BusinessException($"Agenda bloqueada neste período: {block.Reason}.");
                }

                skipped.Add(start);
                continue;
            }

            await EnsureNoConflictAsync(professionalId, input.ClientId, start, end, ignoreIds: [], ct);

            var appointment = new Appointment
            {
                ClientId = input.ClientId,
                ProfessionalId = professionalId,
                Modality = input.Modality,
                ScheduledAt = start,
                DurationMinutes = input.DurationMinutes,
                Observation = Clean(input.Observation),
                SeriesId = seriesId,
                MeetingUrl = meetingUrl,
                ServiceTypeId = input.ServiceTypeId,
                Payment = new Payment { Price = input.Price },
            };

            if (package is not null && packageSlots > 0 && package.IsAvailable(start))
            {
                // Sessão coberta pelo pacote: a cobrança fica no pacote.
                appointment.Package = package;
                appointment.Payment.Price = 0;
                packageSlots--;
            }
            else if (created.Count == 0 && input.PaidPrice > 0)
            {
                appointment.Payment.Entries.Add(new PaymentEntry
                {
                    Amount = input.PaidPrice,
                    PaidAt = Now.Date,
                    Method = input.PaymentMethod,
                    CreatedAt = Now,
                });
            }

            appointment.Payment.SyncFromEntries(Now);
            created.Add(appointment);
        }

        if (created.Count == 0)
        {
            throw new BusinessException("Todas as datas da série caem em períodos bloqueados.");
        }

        db.Appointments.AddRange(created);
        var seriesNote = created.Count > 1
            ? $"Sessões {(input.Recurrence == RecurrenceFrequency.Quinzenal ? "quinzenais" : "semanais")}: {created.Count} encontros agendados."
            : null;
        await NotifyAsync(created[0], MessageKind.Agendamento, seriesNote, ct);
        await db.SaveChangesAsync(ct);

        var warnings = new List<string>();
        var covered = created.Count(a => a.Package is not null);
        if (package is not null && covered < created.Count)
        {
            warnings.Add(covered == 0
                ? "O pacote não tinha sessões disponíveis nestas datas: as sessões foram cobradas avulsas."
                : $"O pacote cobriu {covered} de {created.Count} sessões; as demais foram cobradas avulsas.");
        }

        if (skipped.Count > 0)
        {
            warnings.Add($"Datas puladas por bloqueio de agenda: {string.Join(", ", skipped.Select(d => d.ToString("dd/MM")))}.");
        }

        await AddAvailabilityWarningAsync(professionalId, created, warnings, ct);
        return new ScheduleResult(created.Count, warnings);
    }

    public async Task<ScheduleResult> UpdateAsync(int id, AppointmentInput input, SeriesScope scope = SeriesScope.ApenasEsta, CancellationToken ct = default)
    {
        var appointment = await LoadEditableAsync(id, ct);
        var professionalId = await ResolveProfessionalAsync(input.ProfessionalId ?? appointment.ProfessionalId, ct);
        await ValidateInputAsync(input, ct);

        var targets = await TargetsAsync(appointment, scope, ct);
        var shift = input.ScheduledAt - appointment.ScheduledAt;
        var ids = targets.Select(t => t.Id).ToArray();
        var previousStatus = appointment.Status;

        foreach (var target in targets)
        {
            var isCurrent = target.Id == appointment.Id;
            var start = isCurrent ? input.ScheduledAt : target.ScheduledAt + shift;
            var end = start.AddMinutes(input.DurationMinutes);

            if (target.OccupiesSlot)
            {
                await EnsureNoConflictAsync(professionalId, input.ClientId, start, end, ids, ct);
            }

            if (target.ScheduledAt != start)
            {
                target.ReminderQueuedAt = null; // novo horário → novo lembrete
            }

            target.ClientId = input.ClientId;
            target.ProfessionalId = professionalId;
            target.ScheduledAt = start;
            target.DurationMinutes = input.DurationMinutes;
            target.Modality = input.Modality;
            target.MeetingUrl = input.Modality == AppointmentModality.Teleconsulta ? target.MeetingUrl ?? NewMeetingUrl() : null;

            if (isCurrent)
            {
                target.Observation = Clean(input.Observation);
                target.ServiceTypeId = input.ServiceTypeId;
                ApplyPrice(target, input.Price);
                if (input.Status is { } status && status != target.Status)
                {
                    ApplyStatus(target, status, input.CancellationReason);
                }
            }
            else if (target.Payment is { PaidPrice: 0 } pending)
            {
                // Nas próximas sessões, o novo valor só é aplicado enquanto nada foi pago.
                ApplyPrice(target, input.Price);
            }
        }

        if (appointment.Status == AppointmentStatus.Cancelada && previousStatus != AppointmentStatus.Cancelada)
        {
            await NotifyAsync(appointment, MessageKind.Cancelamento, null, ct);
        }
        else if (shift != TimeSpan.Zero && appointment.OccupiesSlot)
        {
            await NotifyAsync(appointment, MessageKind.Remarcacao, targets.Count > 1 ? $"As próximas {targets.Count - 1} sessões da série também mudaram de horário." : null, ct);
        }

        await db.SaveChangesAsync(ct);

        var warnings = new List<string>();
        await AddAvailabilityWarningAsync(professionalId, targets, warnings, ct);
        return new ScheduleResult(targets.Count, warnings);
    }

    public async Task<ScheduleResult> RescheduleAsync(int id, DateTime start, int? durationMinutes, CancellationToken ct = default)
    {
        var appointment = await LoadEditableAsync(id, ct);
        if (appointment.Status is AppointmentStatus.Realizada or AppointmentStatus.Faltou)
        {
            throw new BusinessException("Consultas já realizadas ou com falta registrada não podem ser remarcadas.");
        }

        if (appointment.Status == AppointmentStatus.Cancelada)
        {
            throw new BusinessException("Esta consulta está cancelada. Reabra-a (altere o status para Agendada) antes de remarcar.");
        }

        var duration = durationMinutes ?? appointment.DurationMinutes;
        ValidateDuration(duration);
        var end = start.AddMinutes(duration);

        if (await FindBlockAsync(appointment.ProfessionalId, start, end, ct) is { } block)
        {
            throw new BusinessException($"Agenda bloqueada neste período: {block.Reason}.");
        }

        await EnsureNoConflictAsync(appointment.ProfessionalId, appointment.ClientId, start, end, [appointment.Id], ct);

        var moved = appointment.ScheduledAt != start;
        appointment.ScheduledAt = start;
        appointment.DurationMinutes = duration;
        if (moved)
        {
            appointment.ReminderQueuedAt = null;
            if (appointment.Status == AppointmentStatus.Confirmada)
            {
                appointment.Status = AppointmentStatus.Agendada; // nova data precisa de nova confirmação
            }

            await NotifyAsync(appointment, MessageKind.Remarcacao, null, ct);
        }

        await db.SaveChangesAsync(ct);

        var warnings = new List<string>();
        await AddAvailabilityWarningAsync(appointment.ProfessionalId, [appointment], warnings, ct);
        return new ScheduleResult(1, warnings);
    }

    public async Task<ScheduleResult> SetStatusAsync(int id, AppointmentStatus status, string? reason, SeriesScope scope = SeriesScope.ApenasEsta, CancellationToken ct = default)
    {
        var appointment = await LoadEditableAsync(id, ct);
        var targets = await TargetsAsync(appointment, scope, ct);

        var changed = targets.Where(t => t.Status != status).ToList();
        foreach (var target in targets)
        {
            ApplyStatus(target, status, reason);
        }

        if (status == AppointmentStatus.Cancelada && changed.Count > 0)
        {
            await NotifyAsync(changed[0], MessageKind.Cancelamento,
                changed.Count > 1 ? $"As próximas {changed.Count - 1} sessões da série também foram canceladas." : null, ct);
        }

        await db.SaveChangesAsync(ct);
        return new ScheduleResult(targets.Count, []);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var appointment = await LoadEditableAsync(id, ct);
        db.Appointments.Remove(appointment); // pagamento removido em cascata
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private Task<int?> RestrictedAsync(CancellationToken ct) =>
        scope?.RestrictedProfessionalIdAsync(ct) ?? Task.FromResult<int?>(null);

    /// <summary>Profissionais com acesso restrito sempre agendam para si mesmos.</summary>
    private async Task<int?> ResolveProfessionalAsync(int? requested, CancellationToken ct)
    {
        if (await RestrictedAsync(ct) is { } own)
        {
            return own;
        }

        if (requested is { } id && !await db.Professionals.AnyAsync(p => p.Id == id, ct))
        {
            throw new BusinessException("Selecione um profissional válido.");
        }

        return requested;
    }

    private async Task<Appointment> LoadEditableAsync(int id, CancellationToken ct)
    {
        var appointment = await db.Appointments.Include(a => a.Payment).FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new BusinessException("Agendamento não encontrado.");

        if (await RestrictedAsync(ct) is { } own && appointment.ProfessionalId != own)
        {
            throw new BusinessException("Você só pode alterar consultas da sua própria agenda.");
        }

        appointment.Payment ??= new Payment(); // consultas legadas podem não ter pagamento
        return appointment;
    }

    private async Task<List<Appointment>> TargetsAsync(Appointment appointment, SeriesScope scope, CancellationToken ct)
    {
        if (scope == SeriesScope.ApenasEsta || appointment.SeriesId is null)
        {
            return [appointment];
        }

        var following = await db.Appointments.Include(a => a.Payment)
            .Where(a => a.SeriesId == appointment.SeriesId && a.ScheduledAt > appointment.ScheduledAt
                        && a.Status != AppointmentStatus.Realizada && a.Status != AppointmentStatus.Faltou)
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync(ct);

        return [appointment, .. following];
    }

    private async Task ValidateInputAsync(AppointmentInput input, CancellationToken ct)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == input.ClientId && c.AnonymizedAt == null, ct))
        {
            throw new BusinessException("Selecione um paciente válido.");
        }

        if (input.PaidPrice > input.Price)
        {
            throw new BusinessException("O valor pago não pode ser maior que o valor da consulta.");
        }

        if (input.ServiceTypeId is { } serviceTypeId && !await db.ServiceTypes.AnyAsync(s => s.Id == serviceTypeId, ct))
        {
            throw new BusinessException("Selecione um tipo de serviço válido.");
        }

        ValidateDuration(input.DurationMinutes);
    }

    private static void ValidateDuration(int minutes)
    {
        if (minutes is < 10 or > Appointment.MaxDurationMinutes)
        {
            throw new BusinessException($"A duração deve ficar entre 10 e {Appointment.MaxDurationMinutes} minutos.");
        }
    }

    /// <summary>Impede dois atendimentos simultâneos do mesmo profissional ou do mesmo paciente.</summary>
    private async Task EnsureNoConflictAsync(int? professionalId, int clientId, DateTime start, DateTime end, int[] ignoreIds, CancellationToken ct)
    {
        // Filtro amplo no banco (janela da duração máxima) e verificação exata em memória: portável entre provedores.
        var windowStart = start.AddMinutes(-Appointment.MaxDurationMinutes);
        var candidates = await db.Appointments.AsNoTracking()
            .Where(a => a.ScheduledAt < end && a.ScheduledAt > windowStart
                        && a.Status != AppointmentStatus.Cancelada
                        && !ignoreIds.Contains(a.Id)
                        && (a.ClientId == clientId || (professionalId != null && a.ProfessionalId == professionalId)))
            .ToListAsync(ct);

        var conflict = candidates.FirstOrDefault(a => a.Overlaps(start, end));
        if (conflict is not null)
        {
            var who = conflict.ClientId == clientId ? "O paciente já tem" : "O profissional já tem";
            throw new BusinessException(
                $"{who} consulta em {conflict.ScheduledAt:dd/MM 'às' HH:mm} ({conflict.ScheduledAt:HH:mm}–{conflict.EndsAt:HH:mm}).");
        }
    }

    private Task<ScheduleBlock?> FindBlockAsync(int? professionalId, DateTime start, DateTime end, CancellationToken ct) =>
        db.ScheduleBlocks.AsNoTracking()
            .Where(b => b.StartsAt < end && b.EndsAt > start && (b.ProfessionalId == null || b.ProfessionalId == professionalId))
            .OrderBy(b => b.StartsAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>Atender fora do horário cadastrado é permitido (encaixes), mas gera um aviso.</summary>
    private async Task AddAvailabilityWarningAsync(int? professionalId, IReadOnlyCollection<Appointment> appointments, List<string> warnings, CancellationToken ct)
    {
        if (professionalId is null)
        {
            return;
        }

        var availability = await db.Availability.AsNoTracking().Where(a => a.ProfessionalId == professionalId).ToListAsync(ct);
        if (availability.Count == 0)
        {
            return;
        }

        var outside = appointments.Count(a => a.OccupiesSlot && !availability.Any(slot => slot.Contains(a.ScheduledAt, a.EndsAt)));
        if (outside > 0)
        {
            warnings.Add(outside == 1
                ? "Atenção: o horário está fora da disponibilidade do profissional."
                : $"Atenção: {outside} sessões ficaram fora da disponibilidade do profissional.");
        }
    }

    /// <summary>
    /// Altera apenas o valor da consulta. Os recebimentos são registrados no Financeiro
    /// (parcelas com data e forma de pagamento); sessões de pacote continuam sem cobrança avulsa.
    /// </summary>
    private void ApplyPrice(Appointment target, decimal price)
    {
        var payment = target.Payment!;
        if (target.PackageId is not null)
        {
            return;
        }

        if (price < payment.PaidPrice)
        {
            throw new BusinessException($"O valor da consulta não pode ser menor que o já recebido ({payment.PaidPrice:C}).");
        }

        payment.Price = price;
        payment.Reconcile(Now);
    }

    /// <summary>Pacote ativo do paciente com sessões disponíveis (o que vence primeiro).</summary>
    private async Task<SessionPackage> FindPackageAsync(int clientId, DateTime start, CancellationToken ct)
    {
        var packages = await db.Packages
            .Include(p => p.Appointments)
            .Where(p => p.ClientId == clientId)
            .ToListAsync(ct);

        return packages
            .Where(p => p.IsAvailable(start))
            .OrderBy(p => p.ExpiresAt ?? DateTime.MaxValue)
            .ThenBy(p => p.PurchasedAt)
            .FirstOrDefault()
            ?? throw new BusinessException("O paciente não tem pacote com sessões disponíveis.");
    }

    private void ApplyStatus(Appointment target, AppointmentStatus status, string? reason)
    {
        if (status is AppointmentStatus.Realizada or AppointmentStatus.Faltou && target.ScheduledAt > Now)
        {
            throw new BusinessException("Só é possível registrar presença ou falta em consultas que já começaram.");
        }

        target.Status = status;
        target.CancellationReason = status == AppointmentStatus.Cancelada ? Clean(reason) : null;
    }

    /// <summary>Avisa o paciente apenas sobre consultas futuras.</summary>
    private async Task NotifyAsync(Appointment appointment, MessageKind kind, string? note, CancellationToken ct)
    {
        if (notifications is not null && appointment.ScheduledAt > Now)
        {
            await notifications.EnqueueAsync(appointment, kind, note, ct);
        }
    }

    private string NewMeetingUrl()
    {
        var baseUrl = (teleconsult?.Value.BaseUrl ?? new TeleconsultOptions().BaseUrl).TrimEnd('/');
        return $"{baseUrl}/mindcare-{Guid.NewGuid():N}";
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
