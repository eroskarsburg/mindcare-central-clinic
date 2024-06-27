using MindCare.Application.Entities;

namespace MindCare_Central_Clinic.Models
{
    public class AppointmentViewModel
    {
        public List<Appointment>? AppointmentList { get; set; }
        public List<Client>? ClientList { get; set; }
    }
}
