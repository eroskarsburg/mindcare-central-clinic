namespace MindCare.Application.Entities
{
    public record Client
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateOnly? Birth { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public Address? Address { get; set; }

        public Client() { }

        public Client(int id, string? name, DateOnly? birth, string? gender, int? age, Address? address)
        {
            Id = id;
            Name = name;
            Birth = birth;
            Gender = gender;
            Age = age;
            Address = address;
        }
    }
}
