namespace MindCare.Application.Entities
{
    public record Client
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? Cpf { get; set; }
        public int? Age { get; set; }
        //public Address? Address { get; set; } -> Not using for now.

        public Client() { }

        public Client(int id, string? name, string? gender, string? cpf, int? age)
        {
            Id = id;
            Name = name;
            Gender = gender;
            Cpf = cpf;
            Age = age;
        }
    }
}
