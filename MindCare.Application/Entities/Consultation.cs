using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public class Consultation
    {
        public ConsultationType? Type { get; set; }
        public int Price { get; set; }
        public string? Observation { get; set; }


        public Consultation(ConsultationType type, int price, string observation)
        {
            Type = type;
            Price = price;
            Observation = observation;
        }
    }
}
