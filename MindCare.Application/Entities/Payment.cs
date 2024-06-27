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
        public EnumPaymentStatus Status { get; set; }
        
        public Client? Client { get; set; }
    }
}
