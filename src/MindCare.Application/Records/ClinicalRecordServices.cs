using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Records;

/// <summary>Evolução decifrada, entregue apenas a quem tem acesso.</summary>
public sealed record NoteView(
    long Id,
    string Content,
    string ProfessionalName,
    bool IsAuthor,
    int? AppointmentId,
    DateTime? AppointmentAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Resultado da listagem: as evoluções visíveis e quantas existem sem acesso (sigilo).</summary>
public sealed record NotesResult(IReadOnlyList<NoteView> Notes, int HiddenCount, bool CanWrite);

public interface IClinicalNoteService
{
    Task<NotesResult> ListAsync(int clientId, CancellationToken ct = default);
    Task<long> CreateAsync(int clientId, int? appointmentId, string content, CancellationToken ct = default);
    Task UpdateAsync(long id, string content, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

/// <summary>
/// Prontuário: só profissionais escrevem; leem o autor e o profissional de referência do paciente.
/// Administradores sem vínculo profissional não têm acesso ao conteúdo (sigilo profissional).
/// </summary>
internal sealed class ClinicalNoteService(
    MindCareDbContext db,
    IAccessScope scope,
    IAuditService audit,
    IDataProtectionProvider protection,
    TimeProvider clock) : IClinicalNoteService
{
    public const string Purpose = "MindCare.ClinicalNotes.v1";
    private const int MaxLength = 20_000;
    private readonly IDataProtector _protector = protection.CreateProtector(Purpose);

    public async Task<NotesResult> ListAsync(int clientId, CancellationToken ct = default)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, ct)
            ?? throw new BusinessException("Paciente não encontrado.");
        var me = await scope.OwnProfessionalIdAsync(ct);

        var notes = await db.ClinicalNotes.AsNoTracking()
            .Include(n => n.Professional)
            .Include(n => n.Appointment)
            .Where(n => n.ClientId == clientId)
            .OrderByDescending(n => n.Appointment != null ? n.Appointment.ScheduledAt : n.CreatedAt)
            .ToListAsync(ct);

        var visible = notes.Where(n => CanRead(n, client, me)).ToList();
        if (visible.Count > 0)
        {
            await audit.LogAsync(AuditActions.Viewed, "Prontuário", clientId.ToString(), $"{visible.Count} evolução(ões)", ct);
        }

        return new NotesResult(
            [.. visible.Select(n => new NoteView(
                n.Id,
                Unprotect(n.EncryptedContent),
                n.Professional?.Name ?? "—",
                n.ProfessionalId == me,
                n.AppointmentId,
                n.Appointment?.ScheduledAt,
                n.CreatedAt,
                n.UpdatedAt))],
            notes.Count - visible.Count,
            me is not null);
    }

    public async Task<long> CreateAsync(int clientId, int? appointmentId, string content, CancellationToken ct = default)
    {
        var me = await scope.OwnProfessionalIdAsync(ct)
            ?? throw new BusinessException("Somente profissionais podem registrar evoluções no prontuário.");

        if (!await db.Clients.AnyAsync(c => c.Id == clientId && c.AnonymizedAt == null, ct))
        {
            throw new BusinessException("Paciente não encontrado.");
        }

        if (appointmentId is { } id && !await db.Appointments.AnyAsync(a => a.Id == id && a.ClientId == clientId, ct))
        {
            throw new BusinessException("A sessão selecionada não pertence a este paciente.");
        }

        var note = new ClinicalNote
        {
            ClientId = clientId,
            ProfessionalId = me,
            AppointmentId = appointmentId,
            EncryptedContent = Protect(content),
            CreatedAt = clock.GetLocalNow().DateTime,
        };
        db.ClinicalNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note.Id;
    }

    public async Task UpdateAsync(long id, string content, CancellationToken ct = default)
    {
        var note = await LoadOwnAsync(id, ct);
        note.EncryptedContent = Protect(content);
        note.UpdatedAt = clock.GetLocalNow().DateTime;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        db.ClinicalNotes.Remove(await LoadOwnAsync(id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Só o autor altera ou exclui a própria evolução.</summary>
    private async Task<ClinicalNote> LoadOwnAsync(long id, CancellationToken ct)
    {
        var me = await scope.OwnProfessionalIdAsync(ct);
        var note = await db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new BusinessException("Evolução não encontrada.");
        return note.ProfessionalId == me
            ? note
            : throw new BusinessException("Apenas o autor pode alterar esta evolução.");
    }

    private static bool CanRead(ClinicalNote note, Client client, int? me) =>
        me is not null && (note.ProfessionalId == me || client.ResponsibleProfessionalId == me);

    private string Protect(string content)
    {
        var text = content.Trim();
        if (text.Length == 0)
        {
            throw new BusinessException("Escreva o conteúdo da evolução.");
        }

        if (text.Length > MaxLength)
        {
            throw new BusinessException($"A evolução deve ter até {MaxLength} caracteres.");
        }

        return _protector.Protect(text);
    }

    private string Unprotect(string encrypted)
    {
        try
        {
            return _protector.Unprotect(encrypted);
        }
        catch (CryptographicException)
        {
            return "[Não foi possível decifrar esta evolução: a chave de criptografia não está disponível.]";
        }
    }
}

public sealed record DocumentUpload(string FileName, string ContentType, long Length, Stream Content);

public sealed record DocumentFile(ClientDocument Document, Stream Content);

public interface IClientDocumentService
{
    Task<IReadOnlyList<ClientDocument>> ListAsync(int clientId, CancellationToken ct = default);
    Task<long> UploadAsync(int clientId, DocumentCategory category, string? description, DocumentUpload upload, CancellationToken ct = default);
    Task<DocumentFile> OpenAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

internal sealed class ClientDocumentService(
    MindCareDbContext db,
    IFileStorage storage,
    IAccessScope scope,
    ICurrentActor actor,
    IAuditService audit,
    IOptions<StorageOptions> options,
    TimeProvider clock) : IClientDocumentService
{
    // Tipos aceitos e a assinatura inicial ("magic bytes") de cada um.
    private static readonly Dictionary<string, (string ContentType, byte[][] Signatures)> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ("application/pdf", [[0x25, 0x50, 0x44, 0x46]]),
        [".png"] = ("image/png", [[0x89, 0x50, 0x4E, 0x47]]),
        [".jpg"] = ("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
        [".jpeg"] = ("image/jpeg", [[0xFF, 0xD8, 0xFF]]),
        [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", [[0x50, 0x4B, 0x03, 0x04]]),
    };

    public static IReadOnlyCollection<string> AllowedExtensions => Allowed.Keys;

    public async Task<IReadOnlyList<ClientDocument>> ListAsync(int clientId, CancellationToken ct = default)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, ct)
            ?? throw new BusinessException("Paciente não encontrado.");
        var me = await scope.OwnProfessionalIdAsync(ct);

        var documents = await db.ClientDocuments.AsNoTracking()
            .Where(d => d.ClientId == clientId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        return [.. documents.Where(d => CanAccess(d, client, me))];
    }

    public async Task<long> UploadAsync(int clientId, DocumentCategory category, string? description, DocumentUpload upload, CancellationToken ct = default)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == clientId && c.AnonymizedAt == null, ct))
        {
            throw new BusinessException("Paciente não encontrado.");
        }

        var me = await scope.OwnProfessionalIdAsync(ct);
        if (category == DocumentCategory.Clinico && me is null)
        {
            throw new BusinessException("Somente profissionais podem anexar documentos clínicos.");
        }

        var maxBytes = options.Value.MaxUploadMegabytes * 1024L * 1024L;
        if (upload.Length is <= 0 || upload.Length > maxBytes)
        {
            throw new BusinessException($"O arquivo deve ter até {options.Value.MaxUploadMegabytes} MB.");
        }

        var extension = Path.GetExtension(upload.FileName);
        if (!Allowed.TryGetValue(extension, out var type))
        {
            throw new BusinessException($"Tipo de arquivo não permitido. Envie {string.Join(", ", Allowed.Keys)}.");
        }

        // Confere o conteúdo real do arquivo, não apenas a extensão informada.
        using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, ct);
        if (!MatchesSignature(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), type.Signatures))
        {
            throw new BusinessException("O conteúdo do arquivo não corresponde à extensão informada.");
        }

        var storagePath = $"clients/{clientId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        buffer.Position = 0;
        await storage.SaveAsync(storagePath, buffer, ct);

        var document = new ClientDocument
        {
            ClientId = clientId,
            Category = category,
            FileName = SafeFileName(upload.FileName),
            StoragePath = storagePath,
            ContentType = type.ContentType,
            SizeBytes = buffer.Length,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UploadedByUserId = actor.UserId ?? 0,
            UploadedByProfessionalId = me,
            UploadedAt = clock.GetLocalNow().DateTime,
        };
        db.ClientDocuments.Add(document);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Delete(storagePath); // não deixa arquivo órfão
            throw;
        }

        return document.Id;
    }

    public async Task<DocumentFile> OpenAsync(long id, CancellationToken ct = default)
    {
        var (document, _) = await LoadAccessibleAsync(id, ct);
        await audit.LogAsync(AuditActions.Viewed, Audit.EntityName<ClientDocument>(), id.ToString(), document.FileName, ct);
        return new DocumentFile(document, storage.OpenRead(document.StoragePath));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var (document, _) = await LoadAccessibleAsync(id, ct);
        var tracked = await db.ClientDocuments.FirstAsync(d => d.Id == id, ct);
        db.ClientDocuments.Remove(tracked);
        await db.SaveChangesAsync(ct);
        storage.Delete(document.StoragePath);
    }

    private async Task<(ClientDocument Document, Client Client)> LoadAccessibleAsync(long id, CancellationToken ct)
    {
        var document = await db.ClientDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new BusinessException("Documento não encontrado.");
        var client = await db.Clients.AsNoTracking().FirstAsync(c => c.Id == document.ClientId, ct);
        var me = await scope.OwnProfessionalIdAsync(ct);
        return CanAccess(document, client, me)
            ? (document, client)
            : throw new BusinessException("Você não tem acesso a este documento.");
    }

    /// <summary>Administrativos: toda a equipe. Clínicos: quem anexou e o profissional de referência.</summary>
    private static bool CanAccess(ClientDocument d, Client client, int? me) =>
        d.Category == DocumentCategory.Administrativo
        || (me is not null && (d.UploadedByProfessionalId == me || client.ResponsibleProfessionalId == me));

    private static bool MatchesSignature(ReadOnlySpan<byte> content, byte[][] signatures)
    {
        foreach (var signature in signatures)
        {
            if (content.StartsWith(signature))
            {
                return true;
            }
        }

        return false;
    }

    private static string SafeFileName(string name)
    {
        var file = Path.GetFileName(name);
        var cleaned = new string([.. file.Where(c => !Path.GetInvalidFileNameChars().Contains(c))]).Trim();
        return cleaned.Length switch
        {
            0 => "documento",
            > 150 => cleaned[..150],
            _ => cleaned,
        };
    }
}
