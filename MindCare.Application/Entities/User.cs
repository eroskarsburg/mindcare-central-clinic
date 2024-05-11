using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record User
    {
        public int Identifier { get; set; 
        public string? Username { get; set; }
        public string? Password { get; set; }
        public EnumAccessLevel AccessLevel { get; set; }
    }
}
