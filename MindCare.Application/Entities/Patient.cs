namespace MindCare.Application.Entities
{
    public class Patient
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public Address? Address { get; set; }
        public Consultation? Appointment { get; set; }


        public Patient() { }

        public Patient(string name, int age, Address address, Consultation? appointment)
        {
            Name = name;
            Age = age;
            Address = address;
            Appointment = appointment;
        }
    }
}
