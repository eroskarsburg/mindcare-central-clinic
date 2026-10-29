using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Persistence;
using MindCare.Application.Records;
using MindCare.Application.Security;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public sealed class RecordTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 5, 4, 10, 0, 0);
    private readonly TestDatabase _database = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(Now, TimeSpan.Zero));
    private readonly EphemeralDataProtectionProvider _protection = new();
    private readonly string _storage = Path.Combine(Path.GetTempPath(), "mindcare-tests", Guid.NewGuid().ToString("N"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Evolucao_e_gravada_criptografada_e_lida_pelo_autor_e_pela_referencia()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);

        await Notes(db, s.LeticiaUser).CreateAsync(s.ClientId, null, "Paciente relata melhora do sono.", Ct);

        var stored = await db.ClinicalNotes.AsNoTracking().SingleAsync(Ct);
        Assert.DoesNotContain("sono", stored.EncryptedContent, StringComparison.Ordinal);

        // Autor (e referência) lê o conteúdo; a leitura é auditada.
        var own = await Notes(db, s.LeticiaUser).ListAsync(s.ClientId, Ct);
        Assert.Equal("Paciente relata melhora do sono.", Assert.Single(own.Notes).Content);
        Assert.Contains(db.AuditLogs, a => a.Entity == "Prontuário" && a.Action == AuditActions.Viewed);

        // Outro profissional não vê o conteúdo, só a contagem oculta.
        var other = await Notes(db, s.MarcosUser).ListAsync(s.ClientId, Ct);
        Assert.Empty(other.Notes);
        Assert.Equal(1, other.HiddenCount);
    }

    [Fact]
    public async Task Administrador_sem_vinculo_profissional_nao_le_nem_escreve()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        await Notes(db, s.LeticiaUser).CreateAsync(s.ClientId, null, "Conteúdo sigiloso.", Ct);

        var admin = Notes(db, s.AdminUser, AccessLevel.Administrador);
        var result = await admin.ListAsync(s.ClientId, Ct);

        Assert.False(result.CanWrite);
        Assert.Empty(result.Notes);
        await Assert.ThrowsAsync<BusinessException>(() => admin.CreateAsync(s.ClientId, null, "x", Ct));
    }

    [Fact]
    public async Task So_o_autor_edita_ou_exclui_a_evolucao()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var id = await Notes(db, s.MarcosUser).CreateAsync(s.ClientId, null, "Avaliação inicial.", Ct);

        // Letícia é a referência: lê, mas não edita o texto do Marcos.
        Assert.Single((await Notes(db, s.LeticiaUser).ListAsync(s.ClientId, Ct)).Notes);
        await Assert.ThrowsAsync<BusinessException>(() => Notes(db, s.LeticiaUser).UpdateAsync(id, "alterado", Ct));

        await Notes(db, s.MarcosUser).UpdateAsync(id, "Avaliação inicial revisada.", Ct);
        var note = Assert.Single((await Notes(db, s.MarcosUser).ListAsync(s.ClientId, Ct)).Notes);
        Assert.Equal(("Avaliação inicial revisada.", true), (note.Content, note.UpdatedAt is not null));
    }

    [Fact]
    public async Task Upload_confere_o_conteudo_real_do_arquivo()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var docs = Documents(db, s.LeticiaUser);

        var fakePdf = new DocumentUpload("laudo.pdf", "application/pdf", 4, new MemoryStream("MZ\x90\x00"u8.ToArray()));
        await Assert.ThrowsAsync<BusinessException>(() => docs.UploadAsync(s.ClientId, DocumentCategory.Clinico, null, fakePdf, Ct));

        var exe = new DocumentUpload("virus.exe", "application/octet-stream", 4, new MemoryStream([1, 2, 3, 4]));
        await Assert.ThrowsAsync<BusinessException>(() => docs.UploadAsync(s.ClientId, DocumentCategory.Administrativo, null, exe, Ct));

        var pdf = "%PDF-1.7 conteúdo"u8.ToArray();
        var id = await docs.UploadAsync(s.ClientId, DocumentCategory.Clinico, "Laudo", new DocumentUpload("../../laudo.pdf", "application/pdf", pdf.Length, new MemoryStream(pdf)), Ct);

        var stored = await db.ClientDocuments.AsNoTracking().SingleAsync(d => d.Id == id, Ct);
        Assert.Equal("laudo.pdf", stored.FileName); // sem caminho
        Assert.StartsWith($"clients/{s.ClientId}/", stored.StoragePath, StringComparison.Ordinal);

        var file = await docs.OpenAsync(id, Ct);
        await using (file.Content)
        {
            using var reader = new StreamReader(file.Content);
            Assert.StartsWith("%PDF", await reader.ReadToEndAsync(Ct), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Documento_clinico_e_restrito_e_administrativo_e_da_equipe()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var pdf = "%PDF-1.7"u8.ToArray();
        DocumentUpload Pdf(string name) => new(name, "application/pdf", pdf.Length, new MemoryStream(pdf));

        var clinical = await Documents(db, s.LeticiaUser).UploadAsync(s.ClientId, DocumentCategory.Clinico, null, Pdf("laudo.pdf"), Ct);
        await Documents(db, s.LeticiaUser).UploadAsync(s.ClientId, DocumentCategory.Administrativo, null, Pdf("termo.pdf"), Ct);

        var admin = Documents(db, s.AdminUser, AccessLevel.Administrador);
        Assert.Equal(["termo.pdf"], (await admin.ListAsync(s.ClientId, Ct)).Select(d => d.FileName));
        await Assert.ThrowsAsync<BusinessException>(() => admin.OpenAsync(clinical, Ct));
        await Assert.ThrowsAsync<BusinessException>(() =>
            admin.UploadAsync(s.ClientId, DocumentCategory.Clinico, null, Pdf("x.pdf"), Ct));

        Assert.Equal(2, (await Documents(db, s.LeticiaUser).ListAsync(s.ClientId, Ct)).Count);
    }

    [Fact]
    public async Task Ficha_calcula_indicadores_do_paciente()
    {
        using var db = _database.CreateContext();
        var s = await SeedAsync(db);
        var appointments = new AppointmentService(db, _clock);
        AppointmentInput Input(DateTime start, decimal paid) =>
            new(s.ClientId, AppointmentModality.Presencial, start, null, 200, paid) { ProfessionalId = s.LeticiaId };

        await appointments.CreateAsync(Input(Now.AddDays(-14), 200), Ct);
        await appointments.CreateAsync(Input(Now.AddDays(-7), 50), Ct);
        await appointments.CreateAsync(Input(Now.AddDays(7), 0), Ct);
        var ids = await db.Appointments.OrderBy(a => a.ScheduledAt).Select(a => a.Id).ToListAsync(Ct);
        await appointments.SetStatusAsync(ids[0], AppointmentStatus.Realizada, null, ct: Ct);
        await appointments.SetStatusAsync(ids[1], AppointmentStatus.Faltou, null, ct: Ct);

        var details = await new ClientService(db, new AuditService(db, new SystemActor(), _clock), _clock).GetDetailsAsync(s.ClientId, Ct);

        Assert.Equal((1, 1), (details.SessionsDone, details.NoShows));
        Assert.Equal(150, details.Outstanding); // só sessões passadas: 0 + 150
        Assert.Equal(ids[2], details.NextAppointment!.Id);
    }

    [Theory]
    [InlineData("1990-05-04", 36)]
    [InlineData("1990-05-05", 35)]
    [InlineData("2000-02-29", 26)]
    public void Idade_e_calculada_pela_data_de_nascimento(string birth, int expected) =>
        Assert.Equal(expected, new Client { BirthDate = DateTime.Parse(birth, System.Globalization.CultureInfo.InvariantCulture), Age = 99 }.AgeOn(Now.Date));

    [Fact]
    public async Task Dashboard_lista_aniversariantes_da_semana()
    {
        using var db = _database.CreateContext();
        db.Clients.AddRange(
            new Client { Name = "Hoje", Cpf = "71428793860", Gender = "Outro", BirthDate = new DateTime(1990, 5, 4) },
            new Client { Name = "Semana", Cpf = "86288366757", Gender = "Outro", BirthDate = new DateTime(1985, 5, 9) },
            new Client { Name = "Longe", Cpf = "39053344705", Gender = "Outro", BirthDate = new DateTime(1985, 6, 20) },
            new Client { Name = "Alta", Cpf = "11144477735", Gender = "Outro", BirthDate = new DateTime(1985, 5, 5), Status = ClientStatus.Alta });
        await db.SaveChangesAsync(Ct);

        var data = await new DashboardService(db, _clock).GetAsync(Ct);

        Assert.Equal([("Hoje", 36), ("Semana", 41)], data.Birthdays.Select(b => (b.Name, b.Age)));
    }

    // ------------------------------------------------------------------

    private sealed record Seed(int ClientId, int LeticiaId, int LeticiaUser, int MarcosUser, int AdminUser);

    private ClinicalNoteService Notes(MindCareDbContext db, int userId, AccessLevel level = AccessLevel.Profissional)
    {
        var actor = new SystemActor { UserId = userId, Username = $"u{userId}", AccessLevel = level };
        return new ClinicalNoteService(db, new AccessScope(db, actor), new AuditService(db, actor, _clock), _protection, _clock);
    }

    private ClientDocumentService Documents(MindCareDbContext db, int userId, AccessLevel level = AccessLevel.Profissional)
    {
        var actor = new SystemActor { UserId = userId, Username = $"u{userId}", AccessLevel = level };
        var options = Options.Create(new StorageOptions { Path = _storage, MaxUploadMegabytes = 1 });
        return new ClientDocumentService(db, new LocalFileStorage(options), new AccessScope(db, actor), actor,
            new AuditService(db, actor, _clock), options, _clock);
    }

    private static async Task<Seed> SeedAsync(MindCareDbContext db)
    {
        Professional NewProfessional(string name, string cpf) => new()
        {
            Name = name,
            Cpf = cpf,
            Gender = "Outro",
            Speciality = "Psicoterapia",
            User = new User { Username = cpf, PasswordHash = "x" },
        };

        var leticia = NewProfessional("Letícia", "15350946056");
        var marcos = NewProfessional("Marcos", "46873529010");
        var admin = new User { Username = "admin", PasswordHash = "x", AccessLevel = AccessLevel.Administrador };
        var client = new Client { Name = "Ana", Cpf = "52998224725", Gender = "Feminino", ResponsibleProfessional = leticia };
        db.AddRange(leticia, marcos, admin, client);
        await db.SaveChangesAsync(Ct);
        return new Seed(client.Id, leticia.Id, leticia.UserId, marcos.UserId, admin.Id);
    }

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_storage))
        {
            Directory.Delete(_storage, recursive: true);
        }
    }
}
