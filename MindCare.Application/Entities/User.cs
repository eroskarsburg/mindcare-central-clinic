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


        public User(int id, string? username, string? password, EnumAccessLevel accessLevel, DateTime lastActivity)
        {
            Id = id;
            Username = username;
            Password = password;
            AccessLevel = accessLevel;
            LastActivity = lastActivity;
        }
    }
}
