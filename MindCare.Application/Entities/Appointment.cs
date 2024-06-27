using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Appointment
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public Payment Payment { get; set; }
        public EnumAppointmentModality? Modality { get; set; }
        public string? ScheduledDate { get; set; }
        public string? ScheduledHour { get; set; }
        public string? Observation { get; set; }
        public List<Client> AllClients { get; set; }

        public Appointment()
        {

        }

        
    }
}
