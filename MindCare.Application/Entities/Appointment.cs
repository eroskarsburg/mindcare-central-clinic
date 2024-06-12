using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Appointment
    {
        public Client Client { get; set; }
        public EnumAppointmentType? Type { get; set; }
        public int Price { get; set; }
        public string? Observation { get; set; }
        public DateOnly ScheduledDate { get; set; }


        public Appointment(Client client, EnumAppointmentType type, int price, string observation, DateOnly scheduledDate)
        {
            Client = client;
            Type = type;
            Price = price;
            Observation = observation;
            ScheduledDate = scheduledDate;
        }
    }
}
