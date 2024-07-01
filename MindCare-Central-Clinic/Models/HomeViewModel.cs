using MindCare.Application.Entities;

namespace MindCare_Central_Clinic.Models
{
    public class HomeViewModel
    {
        public List<Appointment>? ListAppointments { get; set; }
        public List<Payment>? ListPayments { get; set; }
        public List<Payment>? ListPendingPayments { get; set; }
    }
}
