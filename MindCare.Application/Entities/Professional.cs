using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Professional
    {
        public int Id { get; set; }
        public int IdUser { get; set; }
        public string? Name { get; set; }
        public string? Cpf { get; set; }
        public string? Gender { get; set; }
        public int Age { get; set; }
        public EnumAccessLevel AccessLevel { get; set; }

        public Professional()
        {
            
        }
    }
}
