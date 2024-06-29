using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IUserService
    {
        Task<List<User>> GetUsers();
        Task<User> GetUser(User user);
        Task<User> GetUser(int id = 0, string username = "");
        Task InsertUser(User user);
        Task UpdateUser(User user);
        Task DeleteUser(int userId);
    }
}
