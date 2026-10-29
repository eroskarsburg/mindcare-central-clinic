namespace MindCare.Application.Domain;

public enum MessageChannel
{
    WhatsApp,
    Email,
}

/// <summary>Motivo da mensagem ao paciente.</summary>
public enum MessageKind
{
    Agendamento,
    Remarcacao,
    Cancelamento,
    Lembrete,
}

public enum MessageStatus
{
    Pendente,
    Enviada,
    Entregue,
    Lida,
    Falhou,
    Ignorada,
}

/// <summary>
/// Mensagem para o paciente na fila de saída (padrão outbox): gravada junto com a alteração da consulta
/// e enviada depois por um processo em segundo plano, com novas tentativas em caso de falha.
/// </summary>
public sealed class OutboundMessage
{
    public const int MaxAttempts = 5;

    public long Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public MessageChannel Channel { get; set; }
    public MessageKind Kind { get; set; }

    /// <summary>Telefone (somente dígitos, com DDI) ou e-mail.</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Token de confirmação em texto puro, necessário para montar o link. Apagado após o envio.</summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>Complemento exibido na mensagem (ex.: "e as próximas 3 sessões da série").</summary>
    public string? Note { get; set; }

    public MessageStatus Status { get; set; } = MessageStatus.Pendente;
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    /// <summary>Identificador devolvido pelo provedor (ex.: wamid do WhatsApp), usado para receber status de entrega.</summary>
    public string? ExternalId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }

    public void MarkSent(string? externalId, DateTime now)
    {
        Status = MessageStatus.Enviada;
        ExternalId = externalId;
        SentAt = now;
        LastError = null;
        ConfirmationToken = null;
    }

    /// <summary>Registra a falha e agenda nova tentativa com espera crescente (1, 4, 9, 16 minutos...).</summary>
    public void MarkFailed(string error, DateTime now)
    {
        Attempts++;
        LastError = error.Length > 500 ? error[..500] : error;
        if (Attempts >= MaxAttempts)
        {
            Status = MessageStatus.Falhou;
            ConfirmationToken = null;
        }
        else
        {
            NextAttemptAt = now.AddMinutes(Attempts * Attempts);
        }
    }
}

/// <summary>Aviso interno para a equipe (sino do painel).</summary>
public sealed class StaffNotification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Icon { get; set; } = "bi-bell";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
