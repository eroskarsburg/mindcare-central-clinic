namespace MindCare.Application.Notifications;

public sealed class NotificationOptions
{
    public const string Section = "Notifications";

    /// <summary>Liga o processamento em segundo plano (lembretes e envio da fila).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Endereço público da aplicação, usado nos links enviados ao paciente.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>Com quantas horas de antecedência o lembrete é enviado.</summary>
    public int ReminderHoursBefore { get; set; } = 24;

    /// <summary>Intervalo entre execuções do processador da fila, em segundos.</summary>
    public int WorkerIntervalSeconds { get; set; } = 30;

    /// <summary>Envia confirmação ao agendar e aviso ao remarcar/cancelar (além do lembrete).</summary>
    public bool NotifyOnChanges { get; set; } = true;
}

public enum WhatsAppMode
{
    /// <summary>Não envia nada por WhatsApp.</summary>
    Disabled,

    /// <summary>Registra as mensagens no log como se tivessem sido enviadas (desenvolvimento).</summary>
    Simulated,

    /// <summary>Envia pela WhatsApp Cloud API (Meta).</summary>
    Live,
}

public sealed class WhatsAppOptions
{
    public const string Section = "WhatsApp";

    public WhatsAppMode Mode { get; set; } = WhatsAppMode.Disabled;
    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com";
    public string ApiVersion { get; set; } = "v23.0";

    /// <summary>Phone Number ID do número da clínica no WhatsApp Business.</summary>
    public string? PhoneNumberId { get; set; }

    /// <summary>Token de acesso permanente (usuário do sistema) da Meta.</summary>
    public string? AccessToken { get; set; }

    /// <summary>App Secret, usado para validar a assinatura dos webhooks.</summary>
    public string? AppSecret { get; set; }

    /// <summary>Token definido por você no cadastro do webhook (verificação inicial da Meta).</summary>
    public string? VerifyToken { get; set; }

    public string LanguageCode { get; set; } = "pt_BR";

    /// <summary>Nomes dos templates aprovados na Meta para cada tipo de mensagem.</summary>
    public WhatsAppTemplates Templates { get; set; } = new();

    public bool IsEnabled => Mode != WhatsAppMode.Disabled;
}

public sealed class WhatsAppTemplates
{
    public string Agendamento { get; set; } = "mindcare_agendamento";
    public string Remarcacao { get; set; } = "mindcare_remarcacao";
    public string Cancelamento { get; set; } = "mindcare_cancelamento";
    public string Lembrete { get; set; } = "mindcare_lembrete";
}

public sealed class EmailOptions
{
    public const string Section = "Email";

    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 587;

    /// <summary>Usa STARTTLS/SSL conforme a porta (recomendado).</summary>
    public bool UseTls { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "nao-responda@mindcare.local";
    public string FromName { get; set; } = "MindCare";

    /// <summary>Quando definido, grava os e-mails (.eml) nesta pasta em vez de enviar (desenvolvimento).</summary>
    public string? PickupDirectory { get; set; }
}
