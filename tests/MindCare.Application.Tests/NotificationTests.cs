using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MindCare.Application.Common;
using MindCare.Application.Domain;
using MindCare.Application.Notifications;
using MindCare.Application.Persistence;
using MindCare.Application.Services;

namespace MindCare.Application.Tests;

public sealed class NotificationTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 5, 4, 10, 0, 0);
    private readonly TestDatabase _database = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(Now, TimeSpan.Zero));
    private readonly WhatsAppOptions _whatsApp = new() { Mode = WhatsAppMode.Simulated, AppSecret = "segredo" };
    private readonly EmailOptions _email = new() { Enabled = true };
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Agendar_coloca_mensagens_na_fila_para_os_canais_aceitos()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);

        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);

        var messages = await db.OutboundMessages.AsNoTracking().OrderBy(m => m.Id).ToListAsync(Ct);
        Assert.Equal([MessageChannel.WhatsApp, MessageChannel.Email], messages.Select(m => m.Channel));
        Assert.All(messages, m => Assert.Equal(MessageKind.Agendamento, m.Kind));
        Assert.Equal("5551999990001", messages[0].Recipient);

        // Só o hash do token fica na consulta; o texto puro só existe na mensagem até o envio.
        var appointment = await db.Appointments.AsNoTracking().SingleAsync(Ct);
        Assert.Equal(ConfirmationTokens.Hash(messages[0].ConfirmationToken!), appointment.ConfirmationTokenHash);
    }

    [Fact]
    public async Task Paciente_que_recusou_avisos_nao_recebe_mensagens()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        var client = await db.Clients.SingleAsync(Ct);
        client.NotifyByWhatsApp = false;
        client.NotifyByEmail = false;
        await db.SaveChangesAsync(Ct);

        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);

        Assert.False(await db.OutboundMessages.AnyAsync(Ct));
    }

    [Fact]
    public async Task Lembrete_e_agendado_uma_unica_vez_dentro_da_janela()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        var appointments = new AppointmentService(db, _clock); // sem fila: só a consulta
        await appointments.CreateAsync(Input(clientId, professionalId, Now.AddHours(20)), Ct);
        await appointments.CreateAsync(Input(clientId, professionalId, Now.AddDays(3)), Ct);

        var dispatcher = Dispatcher(db, new FakeChannel(MessageChannel.WhatsApp), new FakeChannel(MessageChannel.Email));
        Assert.Equal(1, await dispatcher.QueueDueRemindersAsync(Ct));
        Assert.Equal(0, await dispatcher.QueueDueRemindersAsync(Ct));

        Assert.Equal(2, await db.OutboundMessages.CountAsync(m => m.Kind == MessageKind.Lembrete, Ct));
    }

    [Fact]
    public async Task Agendar_dentro_da_janela_do_lembrete_nao_duplica_mensagem()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddHours(5)), Ct);

        var dispatcher = Dispatcher(db, new FakeChannel(MessageChannel.WhatsApp), new FakeChannel(MessageChannel.Email));

        Assert.Equal(0, await dispatcher.QueueDueRemindersAsync(Ct));
        Assert.All(await db.OutboundMessages.ToListAsync(Ct), m => Assert.Equal(MessageKind.Agendamento, m.Kind));
    }

    [Fact]
    public async Task Envio_marca_como_enviada_e_descarta_o_token()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);
        var whatsapp = new FakeChannel(MessageChannel.WhatsApp);

        await Dispatcher(db, whatsapp, new FakeChannel(MessageChannel.Email)).DispatchPendingAsync(ct: Ct);

        var sent = await db.OutboundMessages.AsNoTracking().SingleAsync(m => m.Channel == MessageChannel.WhatsApp, Ct);
        Assert.Equal(MessageStatus.Enviada, sent.Status);
        Assert.Null(sent.ConfirmationToken);
        Assert.Equal("wamid.1", sent.ExternalId);
        Assert.Contains("/c/", whatsapp.Sent.Single().ConfirmUrl, StringComparison.Ordinal);
        Assert.Equal("Ana", whatsapp.Sent.Single().TemplateParameters[0]);
    }

    [Fact]
    public async Task Falha_temporaria_reagenda_e_falha_permanente_desiste()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);

        await Dispatcher(db,
            new FakeChannel(MessageChannel.WhatsApp) { Failure = new HttpRequestException("fora do ar") },
            new FakeChannel(MessageChannel.Email) { Failure = new PermanentDeliveryException("e-mail inválido") })
            .DispatchPendingAsync(ct: Ct);

        var whatsapp = await db.OutboundMessages.AsNoTracking().SingleAsync(m => m.Channel == MessageChannel.WhatsApp, Ct);
        Assert.Equal((MessageStatus.Pendente, 1, Now.AddMinutes(1)), (whatsapp.Status, whatsapp.Attempts, whatsapp.NextAttemptAt));

        var email = await db.OutboundMessages.AsNoTracking().SingleAsync(m => m.Channel == MessageChannel.Email, Ct);
        Assert.Equal(MessageStatus.Falhou, email.Status);
        Assert.Equal("e-mail inválido", email.LastError);
    }

    [Fact]
    public async Task Mensagem_de_consulta_cancelada_antes_do_envio_e_ignorada()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        var service = Appointments(db);
        await service.CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);
        await service.SetStatusAsync((await db.Appointments.SingleAsync(Ct)).Id, AppointmentStatus.Cancelada, null, ct: Ct);

        var channel = new FakeChannel(MessageChannel.WhatsApp);
        await Dispatcher(db, channel, new FakeChannel(MessageChannel.Email)).DispatchPendingAsync(ct: Ct);

        // O agendamento é ignorado; o aviso de cancelamento é enviado.
        var kinds = await db.OutboundMessages.AsNoTracking()
            .Where(m => m.Channel == MessageChannel.WhatsApp)
            .Select(m => new { m.Kind, m.Status })
            .ToListAsync(Ct);
        Assert.Contains(kinds, k => k.Kind == MessageKind.Agendamento && k.Status == MessageStatus.Ignorada);
        Assert.Contains(kinds, k => k.Kind == MessageKind.Cancelamento && k.Status == MessageStatus.Enviada);
        Assert.Equal(MessageKind.Cancelamento, Assert.Single(channel.Messages).Kind);
    }

    [Fact]
    public async Task Paciente_confirma_e_cancela_pelo_token()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);
        var token = (await db.OutboundMessages.FirstAsync(Ct)).ConfirmationToken!;
        var responses = Responses(db);

        var confirmed = await responses.RespondAsync(token, PatientAction.Confirm, "link", Ct);
        Assert.Equal(AppointmentStatus.Confirmada, confirmed.Status);

        // Admin e profissional responsável são avisados.
        Assert.Equal(2, await db.StaffNotifications.CountAsync(Ct));

        var cancelled = await responses.RespondAsync(token, PatientAction.Cancel, "WhatsApp", Ct);
        Assert.Equal(AppointmentStatus.Cancelada, cancelled.Status);
        Assert.Null(await responses.FindByTokenAsync(token, Ct)); // link invalidado
        Assert.Contains(db.AuditLogs, a => a.Action == "Cancelamento pelo paciente");
    }

    [Fact]
    public async Task Token_invalido_ou_consulta_passada_nao_permite_resposta()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);
        var token = (await db.OutboundMessages.FirstAsync(Ct)).ConfirmationToken!;

        Assert.Null(await Responses(db).FindByTokenAsync("token-falso", Ct));

        var later = new FixedClock(new DateTimeOffset(Now.AddDays(3), TimeSpan.Zero));
        var lateResponses = new PatientResponseService(db, new StaffNotificationService(db, later), later);
        Assert.False((await lateResponses.FindByTokenAsync(token, Ct))!.CanRespond);
        await Assert.ThrowsAsync<BusinessException>(() => lateResponses.RespondAsync(token, PatientAction.Confirm, "link", Ct));
    }

    [Fact]
    public async Task Webhook_valida_assinatura_e_processa_botao_e_status()
    {
        using var db = _database.CreateContext();
        var (clientId, professionalId) = await SeedAsync(db);
        await Appointments(db).CreateAsync(Input(clientId, professionalId, Now.AddDays(2)), Ct);
        var message = await db.OutboundMessages.FirstAsync(m => m.Channel == MessageChannel.WhatsApp, Ct);
        var token = message.ConfirmationToken!;
        message.MarkSent("wamid.ABC", Now);
        await db.SaveChangesAsync(Ct);

        var webhook = new WhatsAppWebhookService(db, Responses(db), new StaffNotificationService(db, _clock), Options.Create(_whatsApp));
        var body = $$$"""
            {"entry":[{"changes":[{"value":{
              "statuses":[{"id":"wamid.ABC","status":"read"}],
              "messages":[{"from":"5551999990001","type":"button","button":{"payload":"CONFIRMAR:{{{token}}}","text":"Confirmar"}}]
            }}]}]}
            """;
        var signature = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("segredo"), Encoding.UTF8.GetBytes(body))).ToLowerInvariant();

        Assert.False(webhook.IsValidSignature(body, "sha256=00"));
        Assert.False(webhook.IsValidSignature(body + " ", signature));
        Assert.True(webhook.IsValidSignature(body, signature));

        await webhook.ProcessAsync(body, Ct);

        Assert.Equal(AppointmentStatus.Confirmada, (await db.Appointments.AsNoTracking().SingleAsync(Ct)).Status);
        Assert.Equal(MessageStatus.Lida, (await db.OutboundMessages.AsNoTracking().SingleAsync(m => m.ExternalId == "wamid.ABC", Ct)).Status);
    }

    [Fact]
    public void Payload_do_whatsapp_segue_o_formato_da_cloud_api()
    {
        var message = new OutboundMessage { Kind = MessageKind.Lembrete, Recipient = "5551999990001", ConfirmationToken = "tok" };
        var appointment = new Appointment
        {
            ScheduledAt = new DateTime(2026, 5, 6, 14, 30, 0),
            Modality = AppointmentModality.Presencial,
            Client = new Client { Name = "Ana Moura" },
            Professional = new Professional { Name = "Letícia Kasper" },
        };

        var content = MessageRenderer.Render(message, appointment, "MindCare", "https://clinica.exemplo.com/");
        var payload = WhatsAppChannel.BuildPayload(message, content, new WhatsAppOptions());

        Assert.Equal("https://clinica.exemplo.com/c/tok", content.ConfirmUrl);
        Assert.Equal("mindcare_lembrete", payload["template"]!["name"]!.GetValue<string>());
        var components = payload["template"]!["components"]!.AsArray();
        Assert.Equal(6, components[0]!["parameters"]!.AsArray().Count);
        Assert.Equal("CONFIRMAR:tok", components[1]!["parameters"]![0]!["payload"]!.GetValue<string>());
        Assert.Equal("CANCELAR:tok", components[2]!["parameters"]![0]!["payload"]!.GetValue<string>());
        Assert.Contains("14:30", content.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("(51) 99999-8888", "5551999998888", "+55 (51) 99999-8888")]
    [InlineData("51 3333-4444", "555133334444", "+55 (51) 3333-4444")]
    [InlineData("+1 415 555 0100", "14155550100", "+14155550100")]
    public void Telefone_e_normalizado_para_o_formato_internacional(string input, string normalized, string formatted)
    {
        Assert.Equal(normalized, Phone.Normalize(input));
        Assert.Equal(formatted, Phone.Format(input));
    }

    // ------------------------------------------------------------------

    private NotificationOutbox Outbox(MindCareDbContext db) =>
        new(db, Options.Create(new NotificationOptions()), Options.Create(_whatsApp), Options.Create(_email), _clock);

    private AppointmentService Appointments(MindCareDbContext db) => new(db, _clock, notifications: Outbox(db));

    private NotificationDispatcher Dispatcher(MindCareDbContext db, params IMessageChannel[] channels) =>
        new(db, Outbox(db), channels, Options.Create(new NotificationOptions()), Options.Create(new ClinicIdentity()), _clock,
            NullLogger<NotificationDispatcher>.Instance);

    private PatientResponseService Responses(MindCareDbContext db) => new(db, new StaffNotificationService(db, _clock), _clock);

    private static AppointmentInput Input(int clientId, int professionalId, DateTime start) =>
        new(clientId, AppointmentModality.Presencial, start, null, 150, 0) { ProfessionalId = professionalId };

    private static async Task<(int ClientId, int ProfessionalId)> SeedAsync(MindCareDbContext db)
    {
        var client = new Client { Name = "Ana Moura", Cpf = "52998224725", Gender = "Feminino", Phone = "5551999990001", Email = "ana@exemplo.com" };
        var professional = new Professional
        {
            Name = "Letícia",
            Cpf = "15350946056",
            Gender = "Feminino",
            Speciality = "Psicoterapia",
            User = new User { Username = "15350946056", PasswordHash = "x" },
        };
        db.Users.Add(new User { Username = "admin", PasswordHash = "x", AccessLevel = AccessLevel.Administrador });
        db.Clients.Add(client);
        db.Professionals.Add(professional);
        await db.SaveChangesAsync(Ct);
        return (client.Id, professional.Id);
    }

    public void Dispose() => _database.Dispose();

    private sealed class FakeChannel(MessageChannel channel) : IMessageChannel
    {
        public MessageChannel Channel => channel;
        public Exception? Failure { get; init; }
        public List<RenderedMessage> Sent { get; } = [];
        public List<OutboundMessage> Messages { get; } = [];

        public Task<SendResult> SendAsync(OutboundMessage message, RenderedMessage content, CancellationToken ct)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Sent.Add(content);
            Messages.Add(message);
            return Task.FromResult(new SendResult($"wamid.{Sent.Count}"));
        }
    }
}
