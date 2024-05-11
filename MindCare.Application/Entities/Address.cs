namespace MindCare.Application.Entities
{
    public record Address
    {
        public string? Street { get; set; }
        public int Number { get; set; }
        public string? Neighborhood { get; set; }
        public int PostalCode { get; set; } = 0;


        public Address(string street, int number, string neighborhood, int postalCode)
        {
            Street = street;
            Number = number;
            Neighborhood = neighborhood;
            PostalCode = postalCode;
        }
    }
}
