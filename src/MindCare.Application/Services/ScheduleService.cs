using Microsoft.EntityFrameworkCore;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;

namespace MindCare.Application.Services;

public sealed record AvailabilitySlot(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public sealed record BlockInput(int? ProfessionalId, DateTime StartsAt, DateTime EndsAt, string Reason);

public sealed record ProfessionalSchedule(
    Professional Professional,
    IReadOnlyList<ProfessionalAvailability> Availability,
    IReadOnlyList<ScheduleBlock> Blocks);

/// <summary>Horários de atendimento dos profissionais e bloqueios de agenda (férias, feriados).</summary>
public interface IScheduleService
{
    Task<ProfessionalSchedule> GetAsync(int professionalId, CancellationToken ct = default);
    Task SaveAvailabilityAsync(int professionalId, IReadOnlyList<AvailabilitySlot> slots, CancellationToken ct = default);
    Task<int> AddBlockAsync(BlockInput input, CancellationToken ct = default);
    Task DeleteBlockAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduleBlock>> ListBlocksAsync(DateTime from, DateTime to, int? professionalId, CancellationToken ct = default);
}

internal sealed class ScheduleService(MindCareDbContext db, ICurrentActor actor, IAccessScope scope, TimeProvider clock) : IScheduleService
{
    public async Task<ProfessionalSchedule> GetAsync(int professionalId, CancellationToken ct = default)
    {
        await EnsureCanManageAsync(professionalId, ct);

        var professional = await db.Professionals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == professionalId, ct)
            ?? throw new BusinessException("Profissional não encontrado.");

        var availability = await db.Availability.AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .ToListAsync(ct);

        var today = clock.GetLocalNow().Date;
        var blocks = await db.ScheduleBlocks.AsNoTracking()
            .Where(b => (b.ProfessionalId == professionalId || b.ProfessionalId == null) && b.EndsAt >= today)
            .OrderBy(b => b.StartsAt)
            .ToListAsync(ct);

        return new ProfessionalSchedule(professional, availability, blocks);
    }

    public async Task SaveAvailabilityAsync(int professionalId, IReadOnlyList<AvailabilitySlot> slots, CancellationToken ct = default)
    {
        await EnsureCanManageAsync(professionalId, ct);

        foreach (var slot in slots)
        {
            if (slot.EndTime <= slot.StartTime)
            {
                throw new BusinessException($"Horário inválido em {DayNames.Of(slot.DayOfWeek)}: o fim deve ser depois do início.");
            }
        }

        foreach (var day in slots.GroupBy(s => s.DayOfWeek))
        {
            var ordered = day.OrderBy(s => s.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    throw new BusinessException($"Os horários de {DayNames.Of(day.Key)} se sobrepõem.");
                }
            }
        }

        var current = await db.Availability.Where(a => a.ProfessionalId == professionalId).ToListAsync(ct);
        db.Availability.RemoveRange(current);
        db.Availability.AddRange(slots.Select(s => new ProfessionalAvailability
        {
            ProfessionalId = professionalId,
            DayOfWeek = s.DayOfWeek,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
        }));
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> AddBlockAsync(BlockInput input, CancellationToken ct = default)
    {
        if (input.ProfessionalId is { } professionalId)
        {
            await EnsureCanManageAsync(professionalId, ct);
        }
        else if (!actor.IsAdmin)
        {
            throw new BusinessException("Apenas administradores podem bloquear a agenda da clínica inteira.");
        }

        if (input.EndsAt <= input.StartsAt)
        {
            throw new BusinessException("O fim do bloqueio deve ser depois do início.");
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new BusinessException("Informe o motivo do bloqueio.");
        }

        var block = new ScheduleBlock
        {
            ProfessionalId = input.ProfessionalId,
            StartsAt = input.StartsAt,
            EndsAt = input.EndsAt,
            Reason = input.Reason.Trim(),
        };
        db.ScheduleBlocks.Add(block);
        await db.SaveChangesAsync(ct);
        return block.Id;
    }

    public async Task DeleteBlockAsync(int id, CancellationToken ct = default)
    {
        var block = await db.ScheduleBlocks.FindAsync([id], ct) ?? throw new BusinessException("Bloqueio não encontrado.");

        if (block.ProfessionalId is { } professionalId)
        {
            await EnsureCanManageAsync(professionalId, ct);
        }
        else if (!actor.IsAdmin)
        {
            throw new BusinessException("Apenas administradores podem remover bloqueios da clínica inteira.");
        }

        db.ScheduleBlocks.Remove(block);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ScheduleBlock>> ListBlocksAsync(DateTime from, DateTime to, int? professionalId, CancellationToken ct = default)
    {
        var own = await scope.RestrictedProfessionalIdAsync(ct) ?? professionalId;
        return await db.ScheduleBlocks.AsNoTracking()
            .Include(b => b.Professional)
            .Where(b => b.StartsAt < to && b.EndsAt > from && (own == null || b.ProfessionalId == null || b.ProfessionalId == own))
            .OrderBy(b => b.StartsAt)
            .ToListAsync(ct);
    }

    /// <summary>O profissional gerencia a própria agenda; administradores e recepção, a de todos.</summary>
    private async Task EnsureCanManageAsync(int professionalId, CancellationToken ct)
    {
        if (await scope.RestrictedProfessionalIdAsync(ct) is { } own && own != professionalId)
        {
            throw new BusinessException("Você só pode gerenciar a sua própria agenda.");
        }
    }
}
