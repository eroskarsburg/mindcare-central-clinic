using MindCare_Central_Clinic.Shared.Enum;

namespace MindCare_Central_Clinic.Shared.Entities
{
    public class PopUpEntity
    {
        public string? Message { get; set; }
        public EnumPopUpType PopUpType { get; set; }

        public PopUpEntity() { }
    }
}
