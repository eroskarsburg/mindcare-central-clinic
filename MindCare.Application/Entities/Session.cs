namespace MindCare.Application.Entities
{
    public class Session
    {
        public User? User { get; set; }
        public Professional? Professional { get; set; }

        public Session()
        {
            
        }

        public Session(User? user, Professional? professional)
        {
            User = user;
            Professional = professional;
        }
    }
}
