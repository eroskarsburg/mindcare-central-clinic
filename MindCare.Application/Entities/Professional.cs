using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record Professional
    {
        public int Id { get; set; }
        public User User { get; set; }
        public string? Name { get; set; }
        public string? Cpf { get; set; }
        public string? Gender { get; set; }
        public string? Speciality { get; set; }

        public Professional()
        {
            
        }
    }
}
