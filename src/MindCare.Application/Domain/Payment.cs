namespace MindCare.Application.Domain;

/// <summary>Pagamento de uma consulta.</summary>
public sealed class Payment
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public decimal Price { get; set; }
    public decimal PaidPrice { get; set; }
    public DateTime? PaidDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pendente;

    /// <summary>Valor que ainda falta receber.</summary>
    public decimal Outstanding => Math.Max(Price - PaidPrice, 0);

    /// <summary>
    /// Recalcula <see cref="Status"/> e <see cref="PaidDate"/> a partir dos valores informados.
    /// Valor pago igual ou superior ao da consulta confirma o pagamento; valor parcial marca como parcial.
    /// </summary>
    public void Reconcile(DateTime today)
    {
        Status = ResolveStatus(Price, PaidPrice);

        if (PaidPrice <= 0)
        {
            PaidDate = null;
        }
        else
        {
            PaidDate ??= today.Date;
        }
    }

    public static PaymentStatus ResolveStatus(decimal price, decimal paidPrice)
    {
        if (paidPrice >= price)
        {
            return PaymentStatus.Confirmado;
        }

        return paidPrice > 0 ? PaymentStatus.Parcial : PaymentStatus.Pendente;
    }
}
