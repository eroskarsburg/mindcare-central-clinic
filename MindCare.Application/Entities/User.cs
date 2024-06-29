using MindCare.Application.Enums;

namespace MindCare.Application.Entities
{
    public record User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public EnumAccessLevel AccessLevel { get; set; }
        public DateTime LastActivity { get; set; }
        public Professional Professional { get; set; }

        public User()
        {
            
        }

        public User(int id, string? username, string? password, EnumAccessLevel accessLevel, DateTime lastActivity, Professional professional)
        {
            Id = id;
            Username = username;
            Password = password;
            AccessLevel = accessLevel;
            LastActivity = lastActivity;
            Professional = professional;
        }
    }
}
