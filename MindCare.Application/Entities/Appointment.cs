using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Appointment
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public EnumAppointmentModality? Type { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public string? Observation { get; set; }

        public Appointment(int id, Client client, EnumAppointmentModality? type, DateOnly scheduledDate, string? observation)
        {
            Id = id;
            Client = client;
            Type = type;
            ScheduledDate = scheduledDate;
            Observation = observation;
        }
    }
}
