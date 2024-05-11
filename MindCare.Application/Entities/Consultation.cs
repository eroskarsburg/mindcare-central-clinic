using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Consultation
    {
        public EnumConsultationType? Type { get; set; }
        public int Price { get; set; }
        public string? Observation { get; set; }


        public Consultation(EnumConsultationType type, int price, string observation)
        {
            Type = type;
            Price = price;
            Observation = observation;
        }
    }
}
