using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Payment
    {
        public int Id { get; set; }
        public int IdAppointment { get; set; }
        public decimal Price { get; set; }
        public decimal PaidPrice { get; set; }
        public string? PaidDate { get; set; }
        public string? PaidDateFormatted
        {
            get
            {
                return string.IsNullOrEmpty(this.PaidDate) ? "-" : DateTime.Parse(this.PaidDate).ToString("dd/MM/yyyy");
            }
        }
        public EnumPaymentStatus Status { get; set; } = EnumPaymentStatus.Pendente;
        public Client? Client { get; set; }
    }
}
