using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IUserService
    {
        Task<List<User>> GetUsers();
        Task InsertUser(User user);
        Task UpdateUser(User user);
        Task DeleteUser(int userId);
    }
}
