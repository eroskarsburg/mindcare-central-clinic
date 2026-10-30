namespace MindCare.Application.Domain;

public enum PaymentMethod
{
    Pix,
    Cartao,
    Dinheiro,
    Transferencia,
    Convenio,

    /// <summary>Recebimentos anteriores ao controle por forma de pagamento.</summary>
    Outro,
}

public static class PaymentMethods
{
    /// <summary>Nome para exibição (ex.: "Cartão", "Transferência").</summary>
    public static string DisplayName(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cartao => "Cartão",
        PaymentMethod.Transferencia => "Transferência",
        PaymentMethod.Convenio => "Convênio",
        _ => method.ToString(),
    };
}

/// <summary>Tipo de atendimento com duração e preço padrão (ex.: sessão individual, casal, avaliação).</summary>
public sealed class ServiceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = Appointment.DefaultDurationMinutes;
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Recebimento: cada pagamento (de consulta ou de pacote) pode ser quitado em várias parcelas,
/// cada uma com sua data e forma de pagamento.
/// </summary>
public sealed class PaymentEntry
{
    public long Id { get; set; }

    /// <summary>Pagamento de consulta (ou nulo quando o recebimento é de um pacote).</summary>
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    /// <summary>Pacote pago (ou nulo quando o recebimento é de uma consulta).</summary>
    public int? PackageId { get; set; }
    public SessionPackage? Package { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Pix;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Pacote de sessões pré-pagas (ex.: 4 sessões no mês com desconto).</summary>
public sealed class SessionPackage
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal Price { get; set; }
    public DateTime PurchasedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public ICollection<PaymentEntry> Entries { get; } = [];
    public ICollection<Appointment> Appointments { get; } = [];

    public decimal PaidAmount => Entries.Sum(e => e.Amount);

    public int UsedSessions => Appointments.Count(a => a.Status != AppointmentStatus.Cancelada);

    public int RemainingSessions => Math.Max(TotalSessions - UsedSessions, 0);

    public bool IsAvailable(DateTime at) => RemainingSessions > 0 && (ExpiresAt is null || ExpiresAt.Value.Date >= at.Date);
}
